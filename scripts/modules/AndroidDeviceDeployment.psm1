Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function ConvertFrom-AdbDevicesOutput
{
    [CmdletBinding()]
    param([string[]]$Lines)

    $devices = @()
    foreach ($line in @($Lines))
    {
        $text = "$line".Trim()
        if (-not $text -or $text -eq 'List of devices attached' -or $text.StartsWith('*'))
        {
            continue
        }

        if ($text -match '^(\S+)\s+(\S+)(?:\s+.*)?$')
        {
            $devices += [pscustomobject]@{
                Serial = $Matches[1]
                State = $Matches[2]
            }
        }
    }

    return $devices
}

function Select-SingleReadyAndroidDevice
{
    [CmdletBinding()]
    param([object[]]$Devices)

    if ($null -eq $Devices -or $Devices.Length -eq 0)
    {
        throw 'No Android device is connected. Connect one device and enable USB debugging.'
    }

    if ($Devices.Length -gt 1)
    {
        throw 'Multiple Android devices are connected. Leave exactly one target device connected.'
    }

    $device = $Devices[0]
    if ($device.State -ne 'device')
    {
        throw "Android device state is $($device.State). Authorize the device and retry."
    }

    return $device.Serial
}

function Assert-AndroidDeviceCompatibility
{
    [CmdletBinding()]
    param(
        [int]$ApiLevel,
        [string]$Abi
    )

    if ($ApiLevel -lt 25)
    {
        throw "Android API Level 25 or newer is required. Detected: $ApiLevel"
    }

    if ($Abi -ne 'arm64-v8a')
    {
        throw "An ARM64 device is required. Detected ABI: $Abi"
    }
}

function ConvertFrom-AndroidBadgingOutput
{
    [CmdletBinding()]
    param([string[]]$Lines)

    $text = $Lines -join "`n"
    $packageMatch = [regex]::Match(
        $text,
        "(?m)^package:.*name='([^']+)'.*versionCode='(\d+)'.*versionName='([^']+)'"
    )
    $minimumApiMatch = [regex]::Match($text, "(?m)^sdkVersion:'(\d+)'$")
    $targetApiMatch = [regex]::Match($text, "(?m)^targetSdkVersion:'(\d+)'$")
    $nativeCodeMatch = [regex]::Match($text, "(?m)^native-code:\s*(.+)$")
    if (
        -not $packageMatch.Success -or
        -not $minimumApiMatch.Success -or
        -not $targetApiMatch.Success -or
        -not $nativeCodeMatch.Success
    )
    {
        throw 'Could not parse the Android APK metadata.'
    }

    $architectures = @(
        [regex]::Matches($nativeCodeMatch.Groups[1].Value, "'([^']+)'") |
            ForEach-Object { $_.Groups[1].Value }
    )
    return [pscustomobject]@{
        PackageId = $packageMatch.Groups[1].Value
        VersionCode = [int]$packageMatch.Groups[2].Value
        VersionName = $packageMatch.Groups[3].Value
        MinimumApiLevel = [int]$minimumApiMatch.Groups[1].Value
        TargetApiLevel = [int]$targetApiMatch.Groups[1].Value
        Architectures = $architectures
    }
}

function Assert-AndroidApkCompatibility
{
    [CmdletBinding()]
    param([object]$Metadata)

    if ($Metadata.PackageId -ne 'com.keiichiito.coyotebattle')
    {
        throw 'The APK package does not match Coyote Battle.'
    }

    if ($Metadata.VersionName -ne '1.0' -or $Metadata.VersionCode -ne 1)
    {
        throw 'The APK version does not match 1.0 (1).'
    }

    if ($Metadata.MinimumApiLevel -ne 25 -or $Metadata.TargetApiLevel -ne 35)
    {
        throw 'The APK API Levels do not match minimum 25 and target 35.'
    }

    if (
        $Metadata.Architectures.Count -ne 1 -or
        $Metadata.Architectures[0] -ne 'arm64-v8a'
    )
    {
        throw 'The APK must contain ARM64 only.'
    }
}

function Assert-AndroidLaunchOutput
{
    [CmdletBinding()]
    param([string[]]$Lines)

    if (($Lines -join "`n") -notmatch '(?m)Events injected:\s*1')
    {
        throw 'The installed application did not report a successful launch event.'
    }
}

function Wait-AndroidProcess
{
    [CmdletBinding()]
    param(
        [System.Diagnostics.Process]$Process,
        [int]$TimeoutMilliseconds,
        [string]$FailureMessage
    )

    if ($TimeoutMilliseconds -lt 1)
    {
        throw 'A positive Android command timeout is required.'
    }

    if (-not $Process.WaitForExit($TimeoutMilliseconds))
    {
        try
        {
            $Process.Kill()
        }
        finally
        {
            $Process.WaitForExit()
        }

        throw "$FailureMessage Command timed out."
    }

    # Complete asynchronous output reads before their Result properties are used.
    $Process.WaitForExit()
}

function New-AndroidDeploymentEvidence
{
    [CmdletBinding()]
    param(
        [string]$ApkPath,
        [string]$CommitSha,
        [string]$VersionName,
        [int]$VersionCode,
        [string]$Manufacturer,
        [string]$Model,
        [string]$AndroidVersion,
        [int]$ApiLevel,
        [string]$Abi
    )

    $apk = Get-Item -LiteralPath $ApkPath -ErrorAction Stop
    if ($apk.Length -le 0)
    {
        throw 'The APK is empty.'
    }

    if ($CommitSha -notmatch '^[0-9a-fA-F]{40}$')
    {
        throw 'A 40-character commit SHA is required.'
    }

    if (-not $VersionName -or $VersionCode -lt 1)
    {
        throw 'A valid installed application version is required.'
    }

    foreach ($value in @($Manufacturer, $Model, $AndroidVersion, $Abi))
    {
        if ([string]::IsNullOrWhiteSpace($value))
        {
            throw 'Complete device information is required.'
        }
    }

    $hash = Get-FileHash -LiteralPath $apk.FullName -Algorithm SHA256
    return [pscustomobject][ordered]@{
        RecordedAtUtc = [DateTime]::UtcNow.ToString('o')
        ApkFileName = $apk.Name
        ApkSize = $apk.Length
        ApkSha256 = $hash.Hash.ToUpperInvariant()
        CommitSha = $CommitSha.ToLowerInvariant()
        VersionName = $VersionName
        VersionCode = $VersionCode
        Device = [pscustomobject][ordered]@{
            Manufacturer = $Manufacturer
            Model = $Model
            AndroidVersion = $AndroidVersion
            ApiLevel = $ApiLevel
            Abi = $Abi
        }
    }
}

function Assert-AndroidBuildEvidence
{
    [CmdletBinding()]
    param(
        [object]$Evidence,
        [string]$ApkPath,
        [string]$CurrentCommitSha,
        [bool]$HasTrackedChanges
    )

    if ($HasTrackedChanges)
    {
        throw 'The worktree has tracked changes. Build and install an exact commit.'
    }

    if ($Evidence.CommitSha -ne $CurrentCommitSha)
    {
        throw 'The APK build commit does not match the current commit.'
    }

    $apk = Get-Item -LiteralPath $ApkPath -ErrorAction Stop
    $hash = (Get-FileHash -LiteralPath $apk.FullName -Algorithm SHA256).Hash.ToUpperInvariant()
    if ([long]$Evidence.ApkSize -ne $apk.Length -or $Evidence.ApkSha256 -ne $hash)
    {
        throw 'The APK does not match its build evidence.'
    }
}

Export-ModuleMember -Function ConvertFrom-AdbDevicesOutput
Export-ModuleMember -Function Select-SingleReadyAndroidDevice
Export-ModuleMember -Function Assert-AndroidDeviceCompatibility
Export-ModuleMember -Function ConvertFrom-AndroidBadgingOutput
Export-ModuleMember -Function Assert-AndroidApkCompatibility
Export-ModuleMember -Function Assert-AndroidLaunchOutput
Export-ModuleMember -Function Wait-AndroidProcess
Export-ModuleMember -Function New-AndroidDeploymentEvidence
Export-ModuleMember -Function Assert-AndroidBuildEvidence
