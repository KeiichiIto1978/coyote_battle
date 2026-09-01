[CmdletBinding()]
param(
    [string]$UnityPath,
    [string]$ProjectPath,
    [ValidateRange(1, 3600)]
    [int]$TimeoutSeconds = 300
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-UnityEditorPath
{
    param(
        [string]$RequestedPath,
        [string]$ResolvedProjectPath
    )

    if ($RequestedPath)
    {
        if (-not (Test-Path -LiteralPath $RequestedPath -PathType Leaf))
        {
            throw "Specified Unity was not found: $RequestedPath"
        }

        return (Resolve-Path -LiteralPath $RequestedPath).Path
    }

    $projectVersionPath = Join-Path $ResolvedProjectPath 'ProjectSettings\ProjectVersion.txt'
    if (-not (Test-Path -LiteralPath $projectVersionPath -PathType Leaf))
    {
        throw "Unity project was not found: $ResolvedProjectPath"
    }

    $versionLine = Get-Content -LiteralPath $projectVersionPath |
        Where-Object { $_ -match '^m_EditorVersion:\s*(.+)$' } |
        Select-Object -First 1

    if (-not $versionLine)
    {
        throw 'Could not read the Unity version from ProjectVersion.txt.'
    }

    $version = ($versionLine -replace '^m_EditorVersion:\s*', '').Trim()
    $candidate = Join-Path ${env:ProgramFiles} "Unity\Hub\Editor\$version\Editor\Unity.exe"
    if (-not (Test-Path -LiteralPath $candidate -PathType Leaf))
    {
        throw "Unity $version was not found. Specify Unity.exe with -UnityPath."
    }

    return $candidate
}

function Test-ProjectIsOpen
{
    param([string]$ResolvedProjectPath)

    $normalizedProjectPath = $ResolvedProjectPath.TrimEnd('\').ToLowerInvariant()
    $unityProcesses = Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'"

    foreach ($unityProcess in $unityProcesses)
    {
        if ($unityProcess.CommandLine -and
            $unityProcess.CommandLine.ToLowerInvariant().Contains($normalizedProjectPath))
        {
            return $true
        }
    }

    return $false
}

function ConvertTo-QuotedArgument
{
    param([string]$Value)

    $normalizedValue = $Value
    if ($normalizedValue -match '^[A-Za-z]:\\$')
    {
        $normalizedValue += '.'
    }

    return '"' + $normalizedValue.Replace('"', '\"') + '"'
}

try
{
    if (-not $ProjectPath)
    {
        $ProjectPath = Split-Path -Parent $PSScriptRoot
    }

    $resolvedProjectPath = (Resolve-Path -LiteralPath $ProjectPath).Path
    $resolvedUnityPath = Get-UnityEditorPath `
        -RequestedPath $UnityPath `
        -ResolvedProjectPath $resolvedProjectPath

    if (Test-ProjectIsOpen -ResolvedProjectPath $resolvedProjectPath)
    {
        throw 'This project is already open in Unity. Close Unity and run the test again.'
    }

    $resultDirectory = Join-Path $resolvedProjectPath 'TestResults'
    $resultPath = Join-Path $resultDirectory 'PlayMode.xml'
    $logPath = Join-Path $resultDirectory 'PlayMode.log'

    New-Item -ItemType Directory -Path $resultDirectory -Force | Out-Null
    Remove-Item -LiteralPath $resultPath, $logPath -Force -ErrorAction SilentlyContinue

    $unityArguments = @(
        '-batchmode',
        '-nographics',
        '-projectPath',
        (ConvertTo-QuotedArgument $resolvedProjectPath),
        '-runTests',
        '-testPlatform',
        'PlayMode',
        '-testResults',
        (ConvertTo-QuotedArgument $resultPath),
        '-logFile',
        (ConvertTo-QuotedArgument $logPath)
    )

    Write-Output "Running Unity PlayMode tests: $resolvedUnityPath"
    $unityProcess = Start-Process `
        -FilePath $resolvedUnityPath `
        -ArgumentList $unityArguments `
        -WindowStyle Hidden `
        -PassThru

    if (-not $unityProcess.WaitForExit($TimeoutSeconds * 1000))
    {
        $unityProcess.Kill()
        $unityProcess.WaitForExit()
        throw "Unity tests did not finish within ${TimeoutSeconds} seconds. Log: $logPath"
    }

    if ($unityProcess.ExitCode -ne 0)
    {
        throw "Unity failed with exit code $($unityProcess.ExitCode). Log: $logPath"
    }

    if (-not (Test-Path -LiteralPath $resultPath -PathType Leaf))
    {
        throw "Test results were not generated. Log: $logPath"
    }

    [xml]$testResult = Get-Content -Raw -Encoding UTF8 -LiteralPath $resultPath
    $testRun = $testResult.'test-run'
    $total = [int]$testRun.total
    $passed = [int]$testRun.passed
    $failed = [int]$testRun.failed
    $skipped = [int]$testRun.skipped

    if ($total -eq 0)
    {
        throw "No tests were found. Result: $resultPath"
    }

    if ($failed -gt 0)
    {
        throw "PlayMode tests failed (passed: $passed, failed: $failed, skipped: $skipped). Result: $resultPath"
    }

    Write-Output "PlayMode tests succeeded (passed: $passed, failed: $failed, skipped: $skipped)"
    Write-Output "Result: $resultPath"
    exit 0
}
catch
{
    Write-Error $_.Exception.Message
    exit 1
}
