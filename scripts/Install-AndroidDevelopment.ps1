[CmdletBinding()]
param(
    [string]$UnityPath,
    [string]$ProjectPath,
    [string]$ApkPath,
    [string]$AdbPath,
    [string]$AaptPath,
    [string]$BuildEvidencePath,
    [string]$EvidencePath,
    [ValidateRange(1, 3600)]
    [int]$AdbTimeoutSeconds = 60
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$packageId = 'com.keiichiito.coyotebattle'
$script:AndroidToolTimeoutSeconds = $AdbTimeoutSeconds

function Resolve-ProjectPath
{
    param([string]$RequestedPath)

    if ($RequestedPath)
    {
        return (Resolve-Path -LiteralPath $RequestedPath).Path
    }

    return (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
}

function Resolve-UnityPath
{
    param(
        [string]$RequestedPath,
        [string]$ResolvedProjectPath
    )

    if ($RequestedPath)
    {
        return (Resolve-Path -LiteralPath $RequestedPath).Path
    }

    $versionFile = Join-Path $ResolvedProjectPath 'ProjectSettings\ProjectVersion.txt'
    $versionLine = Get-Content -LiteralPath $versionFile |
        Where-Object { $_ -match '^m_EditorVersion:\s*(.+)$' } |
        Select-Object -First 1
    if (-not $versionLine)
    {
        throw 'Could not read the Unity version from ProjectVersion.txt.'
    }

    $version = ($versionLine -replace '^m_EditorVersion:\s*', '').Trim()
    $candidate = Join-Path ${env:ProgramFiles} "Unity\Hub\Editor\$version\Editor\Unity.exe"
    return (Resolve-Path -LiteralPath $candidate).Path
}

function Invoke-AndroidTool
{
    param(
        [string]$ExecutablePath,
        [string[]]$Arguments,
        [string]$FailureMessage
    )

    $quotedArguments = $Arguments | ForEach-Object {
        '"' + $_.Replace('"', '\"') + '"'
    }
    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = $ExecutablePath
    $startInfo.Arguments = $quotedArguments -join ' '
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $startInfo
    $null = $process.Start()
    $standardOutput = $process.StandardOutput.ReadToEndAsync()
    $standardError = $process.StandardError.ReadToEndAsync()
    Wait-AndroidProcess `
        -Process $process `
        -TimeoutMilliseconds ($script:AndroidToolTimeoutSeconds * 1000) `
        -FailureMessage $FailureMessage
    $output = @($standardOutput.Result, $standardError.Result) |
        ForEach-Object { $_ -split "`r?`n" }
    if ($process.ExitCode -ne 0)
    {
        throw $FailureMessage
    }

    return $output
}

function Get-DeviceProperty
{
    param(
        [string]$ResolvedAdbPath,
        [string]$Serial,
        [string]$PropertyName
    )

    $value = Invoke-AndroidTool `
        -ExecutablePath $ResolvedAdbPath `
        -Arguments @('-s', $Serial, 'shell', 'getprop', $PropertyName) `
        -FailureMessage "Could not read Android property: $PropertyName"
    return ($value -join '').Trim()
}

try
{
    $resolvedProjectPath = Resolve-ProjectPath -RequestedPath $ProjectPath
    $resolvedUnityPath = Resolve-UnityPath `
        -RequestedPath $UnityPath `
        -ResolvedProjectPath $resolvedProjectPath
    if (-not $AdbPath)
    {
        $editorData = Join-Path (Split-Path -Parent $resolvedUnityPath) 'Data'
        $AdbPath = Join-Path $editorData 'PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe'
    }
    $resolvedAdbPath = (Resolve-Path -LiteralPath $AdbPath).Path
    if (-not $AaptPath)
    {
        $buildToolsPath = Join-Path (Split-Path -Parent $resolvedAdbPath) '..\build-tools'
        $AaptPath = Get-ChildItem -LiteralPath $buildToolsPath -Filter 'aapt.exe' -Recurse |
            Sort-Object FullName -Descending |
            Select-Object -ExpandProperty FullName -First 1
    }
    $resolvedAaptPath = (Resolve-Path -LiteralPath $AaptPath).Path

    if (-not $ApkPath)
    {
        $ApkPath = Join-Path $resolvedProjectPath 'Builds\Android\CoyoteBattle-development.apk'
    }
    $resolvedApkPath = (Resolve-Path -LiteralPath $ApkPath).Path
    if ((Get-Item -LiteralPath $resolvedApkPath).Length -le 0)
    {
        throw 'The APK is empty.'
    }

    $modulePath = Join-Path $PSScriptRoot 'modules\AndroidDeviceDeployment.psm1'
    Import-Module $modulePath -Force
    if (-not $BuildEvidencePath)
    {
        $BuildEvidencePath = "$resolvedApkPath.build.json"
    }
    $buildEvidence = Get-Content -LiteralPath $BuildEvidencePath -Raw | ConvertFrom-Json
    $gitProjectPath = $resolvedProjectPath
    if ($gitProjectPath -match '^[A-Za-z]:\\$')
    {
        $gitProjectPath += '.'
    }
    $commitShaOutput = & git -C $gitProjectPath rev-parse HEAD 2>$null
    $commitShaExitCode = $LASTEXITCODE
    $commitSha = ($commitShaOutput | Select-Object -First 1).Trim()
    if ($commitShaExitCode -ne 0)
    {
        throw 'Could not determine the current commit SHA.'
    }
    & git -C $gitProjectPath diff-index --quiet HEAD --
    $worktreeExitCode = $LASTEXITCODE
    if ($worktreeExitCode -gt 1)
    {
        throw 'Could not inspect the worktree state.'
    }
    Assert-AndroidBuildEvidence `
        -Evidence $buildEvidence `
        -ApkPath $resolvedApkPath `
        -CurrentCommitSha $commitSha `
        -HasTrackedChanges ($worktreeExitCode -eq 1)

    $badgingOutput = Invoke-AndroidTool `
        -ExecutablePath $resolvedAaptPath `
        -Arguments @('dump', 'badging', $resolvedApkPath) `
        -FailureMessage 'Could not inspect the Android APK.'
    $apkMetadata = ConvertFrom-AndroidBadgingOutput -Lines $badgingOutput
    Assert-AndroidApkCompatibility -Metadata $apkMetadata

    $deviceLines = Invoke-AndroidTool `
        -ExecutablePath $resolvedAdbPath `
        -Arguments @('devices', '-l') `
        -FailureMessage 'Could not enumerate Android devices.'
    $devices = ConvertFrom-AdbDevicesOutput -Lines $deviceLines
    $serial = Select-SingleReadyAndroidDevice -Devices $devices

    $manufacturer = Get-DeviceProperty $resolvedAdbPath $serial 'ro.product.manufacturer'
    $model = Get-DeviceProperty $resolvedAdbPath $serial 'ro.product.model'
    $androidVersion = Get-DeviceProperty $resolvedAdbPath $serial 'ro.build.version.release'
    $apiLevel = [int](Get-DeviceProperty $resolvedAdbPath $serial 'ro.build.version.sdk')
    $abi = Get-DeviceProperty $resolvedAdbPath $serial 'ro.product.cpu.abi'
    Assert-AndroidDeviceCompatibility -ApiLevel $apiLevel -Abi $abi

    Invoke-AndroidTool `
        -ExecutablePath $resolvedAdbPath `
        -Arguments @('-s', $serial, 'install', '-r', $resolvedApkPath) `
        -FailureMessage 'ADB install failed. The existing app was not removed; review signing, version, storage, and the device screen.' |
        Out-Null

    $packageInfo = Invoke-AndroidTool `
        -ExecutablePath $resolvedAdbPath `
        -Arguments @('-s', $serial, 'shell', 'dumpsys', 'package', $packageId) `
        -FailureMessage 'Could not verify the installed package.'
    $packageText = $packageInfo -join "`n"
    $versionNameMatch = [regex]::Match($packageText, '(?m)^\s*versionName=(\S+)')
    $versionCodeMatch = [regex]::Match($packageText, '(?m)^\s*versionCode=(\d+)')
    if (-not $versionNameMatch.Success -or -not $versionCodeMatch.Success)
    {
        throw 'Could not read the installed package version.'
    }
    if (
        $versionNameMatch.Groups[1].Value -ne $apkMetadata.VersionName -or
        [int]$versionCodeMatch.Groups[1].Value -ne $apkMetadata.VersionCode
    )
    {
        throw 'The installed package version does not match the inspected APK.'
    }

    $launchOutput = Invoke-AndroidTool `
        -ExecutablePath $resolvedAdbPath `
        -Arguments @('-s', $serial, 'shell', 'monkey', '-p', $packageId, '-c', 'android.intent.category.LAUNCHER', '1') `
        -FailureMessage 'The installed application could not be launched.'
    Assert-AndroidLaunchOutput -Lines $launchOutput

    $evidence = Get-AndroidDeploymentEvidence `
        -ApkPath $resolvedApkPath `
        -CommitSha $commitSha `
        -VersionName $versionNameMatch.Groups[1].Value `
        -VersionCode ([int]$versionCodeMatch.Groups[1].Value) `
        -Manufacturer $manufacturer `
        -Model $model `
        -AndroidVersion $androidVersion `
        -ApiLevel $apiLevel `
        -Abi $abi
    if (-not $EvidencePath)
    {
        $EvidencePath = Join-Path $resolvedProjectPath 'TestResults\AndroidDeviceEvidence.json'
    }
    $resolvedEvidencePath = [System.IO.Path]::GetFullPath($EvidencePath)
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $resolvedEvidencePath) |
        Out-Null
    $evidenceJson = $evidence | ConvertTo-Json -Depth 4
    [System.IO.File]::WriteAllText(
        $resolvedEvidencePath,
        $evidenceJson,
        (New-Object System.Text.UTF8Encoding($false))
    )

    Write-Output "Installed and launched $packageId $($evidence.VersionName) ($($evidence.VersionCode))."
    Write-Output "Device: $manufacturer $model / Android $androidVersion / API $apiLevel / $abi"
    Write-Output "APK SHA-256: $($evidence.ApkSha256)"
    Write-Output "Evidence: $resolvedEvidencePath"
}
catch
{
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}
