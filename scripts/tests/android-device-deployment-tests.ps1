Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Prevent Issue #11 deployment from modifying the wrong or unauthorized device.
$modulePath = Join-Path $PSScriptRoot '..\modules\AndroidDeviceDeployment.psm1'
Import-Module $modulePath -Force

function Assert-Equal
{
    param(
        $Actual,
        $Expected,
        [string]$Message
    )

    if ($Actual -ne $Expected)
    {
        throw "$Message Expected=[$Expected] Actual=[$Actual]"
    }
}

function Assert-Throws
{
    param(
        [scriptblock]$Action,
        [string]$Pattern,
        [string]$Message
    )

    try
    {
        & $Action
    }
    catch
    {
        if ($_.Exception.Message -notlike "*$Pattern*")
        {
            throw "$Message Unexpected=[$($_.Exception.Message)]"
        }

        return
    }

    throw "$Message No exception was thrown."
}

$oneDevice = ConvertFrom-AdbDevicesOutput -Lines @(
    'List of devices attached',
    'secret-serial device product:test model:Pixel_8 device:husky transport_id:1'
)
Assert-Equal (@($oneDevice).Count) 1 'Failed to parse one connected device.'
Assert-Equal $oneDevice[0].State 'device' 'Failed to parse the ready state.'
Assert-Equal (Select-SingleReadyAndroidDevice -Devices $oneDevice) 'secret-serial' 'Failed to select one device.'

Assert-Throws {
    Select-SingleReadyAndroidDevice -Devices @()
} 'No Android device is connected' 'Zero devices were accepted.'

$twoDevices = ConvertFrom-AdbDevicesOutput -Lines @(
    'List of devices attached',
    'first device product:test',
    'second device product:test'
)
Assert-Throws {
    Select-SingleReadyAndroidDevice -Devices $twoDevices
} 'Multiple Android devices' 'Multiple devices were accepted.'

foreach ($state in @('unauthorized', 'offline'))
{
    $unavailable = ConvertFrom-AdbDevicesOutput -Lines @(
        'List of devices attached',
        "secret-serial $state product:test"
    )
    Assert-Throws {
        Select-SingleReadyAndroidDevice -Devices $unavailable
    } $state "$state device was accepted."
}

Assert-AndroidDeviceCompatibility -ApiLevel 25 -Abi 'arm64-v8a'
Assert-Throws {
    Assert-AndroidDeviceCompatibility -ApiLevel 24 -Abi 'arm64-v8a'
} 'API Level 25' 'API 24 was accepted.'
Assert-Throws {
    Assert-AndroidDeviceCompatibility -ApiLevel 35 -Abi 'armeabi-v7a'
} 'ARM64' 'ARMv7 was accepted.'

$validBadging = @(
    "package: name='com.keiichiito.coyotebattle' versionCode='1' versionName='1.0'",
    "sdkVersion:'25'",
    "targetSdkVersion:'35'",
    "native-code: 'arm64-v8a'"
)
$apkMetadata = ConvertFrom-AndroidBadgingOutput -Lines $validBadging
Assert-AndroidApkCompatibility -Metadata $apkMetadata
Assert-Equal $apkMetadata.PackageId 'com.keiichiito.coyotebattle' 'APK package is incorrect.'
Assert-Equal $apkMetadata.VersionCode 1 'APK versionCode is incorrect.'

$wrongPackage = ConvertFrom-AndroidBadgingOutput -Lines @(
    "package: name='com.example.other' versionCode='1' versionName='1.0'",
    "sdkVersion:'25'",
    "targetSdkVersion:'35'",
    "native-code: 'arm64-v8a'"
)
Assert-Throws {
    Assert-AndroidApkCompatibility -Metadata $wrongPackage
} 'package does not match' 'An unrelated APK was accepted.'

$wrongArchitecture = ConvertFrom-AndroidBadgingOutput -Lines @(
    "package: name='com.keiichiito.coyotebattle' versionCode='1' versionName='1.0'",
    "sdkVersion:'25'",
    "targetSdkVersion:'35'",
    "native-code: 'armeabi-v7a'"
)
Assert-Throws {
    Assert-AndroidApkCompatibility -Metadata $wrongArchitecture
} 'ARM64 only' 'An APK without ARM64-only native code was accepted.'

Assert-Throws {
    ConvertFrom-AndroidBadgingOutput -Lines @('badging output is incomplete')
} 'Could not parse' 'Invalid APK metadata was accepted.'

Assert-AndroidLaunchOutput -Lines @('Events injected: 1')
Assert-Throws {
    Assert-AndroidLaunchOutput -Lines @('No activities found to run')
} 'successful launch event' 'A failed launch was accepted.'

$currentPowerShell = (Get-Process -Id $PID).Path
$slowProcess = New-Object System.Diagnostics.Process
$slowProcess.StartInfo = New-Object System.Diagnostics.ProcessStartInfo
$slowProcess.StartInfo.FileName = $currentPowerShell
$slowProcess.StartInfo.Arguments = '-NoProfile -Command "Start-Sleep -Seconds 5"'
$slowProcess.StartInfo.UseShellExecute = $false
$null = $slowProcess.Start()
try
{
    Assert-Throws {
        Wait-AndroidProcess `
            -Process $slowProcess `
            -TimeoutMilliseconds 50 `
            -FailureMessage 'ADB command failed.'
    } 'Command timed out' 'A timed out Android command was accepted.'
    Assert-Equal $slowProcess.HasExited $true 'The timed out process was not stopped.'
}
finally
{
    if (-not $slowProcess.HasExited)
    {
        $slowProcess.Kill()
        $slowProcess.WaitForExit()
    }

    $slowProcess.Dispose()
}

$temporaryApk = Join-Path ([System.IO.Path]::GetTempPath()) "coyote-apk-$([Guid]::NewGuid().ToString('N')).apk"
try
{
    [System.IO.File]::WriteAllBytes($temporaryApk, [byte[]](1, 2, 3, 4))
    $evidence = New-AndroidDeploymentEvidence `
        -ApkPath $temporaryApk `
        -CommitSha ('a' * 40) `
        -VersionName '1.0' `
        -VersionCode 1 `
        -Manufacturer 'Google' `
        -Model 'Pixel 8' `
        -AndroidVersion '15' `
        -ApiLevel 35 `
        -Abi 'arm64-v8a'
    $json = $evidence | ConvertTo-Json -Depth 3
    Assert-Equal $evidence.ApkSize 4 'APK size is incorrect.'
    Assert-Equal $evidence.CommitSha ('a' * 40) 'Commit SHA is incorrect.'
    if ($evidence.ApkSha256 -notmatch '^[A-F0-9]{64}$')
    {
        throw 'APK SHA-256 is not a 64-character hexadecimal value.'
    }
    if ($json -match 'secret-serial|Serial')
    {
        throw 'Deployment evidence contains an ADB serial.'
    }

    $buildEvidence = [pscustomobject]@{
        CommitSha = 'a' * 40
        ApkSize = 4
        ApkSha256 = $evidence.ApkSha256
    }
    Assert-AndroidBuildEvidence `
        -Evidence $buildEvidence `
        -ApkPath $temporaryApk `
        -CurrentCommitSha ('a' * 40) `
        -HasTrackedChanges $false
    Assert-Throws {
        Assert-AndroidBuildEvidence `
            -Evidence $buildEvidence `
            -ApkPath $temporaryApk `
            -CurrentCommitSha ('b' * 40) `
            -HasTrackedChanges $false
    } 'commit does not match' 'Evidence for a different commit was accepted.'
    Assert-Throws {
        Assert-AndroidBuildEvidence `
            -Evidence $buildEvidence `
            -ApkPath $temporaryApk `
            -CurrentCommitSha ('a' * 40) `
            -HasTrackedChanges $true
    } 'tracked changes' 'A dirty tracked worktree was accepted.'
}
finally
{
    Remove-Item -LiteralPath $temporaryApk -Force -ErrorAction SilentlyContinue
}

Write-Output 'Android device deployment tests passed.'
