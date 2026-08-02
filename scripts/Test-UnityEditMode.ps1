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
            throw "指定されたUnityが見つかりません: $RequestedPath"
        }

        return (Resolve-Path -LiteralPath $RequestedPath).Path
    }

    $projectVersionPath = Join-Path $ResolvedProjectPath 'ProjectSettings\ProjectVersion.txt'
    if (-not (Test-Path -LiteralPath $projectVersionPath -PathType Leaf))
    {
        throw "Unityプロジェクトではありません: $ResolvedProjectPath"
    }

    $versionLine = Get-Content -LiteralPath $projectVersionPath |
        Where-Object { $_ -match '^m_EditorVersion:\s*(.+)$' } |
        Select-Object -First 1

    if (-not $versionLine)
    {
        throw 'ProjectVersion.txtからUnityバージョンを取得できません。'
    }

    $version = ($versionLine -replace '^m_EditorVersion:\s*', '').Trim()
    $candidate = Join-Path ${env:ProgramFiles} "Unity\Hub\Editor\$version\Editor\Unity.exe"
    if (-not (Test-Path -LiteralPath $candidate -PathType Leaf))
    {
        throw "Unity $version が見つかりません。-UnityPathでUnity.exeを指定してください。"
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

    return '"' + $Value.Replace('"', '\"') + '"'
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
        throw 'このプロジェクトは既にUnityで開かれています。Unityを終了してから再実行してください。'
    }

    $resultDirectory = Join-Path $resolvedProjectPath 'TestResults'
    $resultPath = Join-Path $resultDirectory 'EditMode.xml'
    $logPath = Join-Path $resultDirectory 'EditMode.log'

    New-Item -ItemType Directory -Path $resultDirectory -Force | Out-Null
    Remove-Item -LiteralPath $resultPath, $logPath -Force -ErrorAction SilentlyContinue

    $unityArguments = @(
        '-batchmode',
        '-nographics',
        '-projectPath',
        (ConvertTo-QuotedArgument $resolvedProjectPath),
        '-runTests',
        '-testPlatform',
        'EditMode',
        '-testResults',
        (ConvertTo-QuotedArgument $resultPath),
        '-logFile',
        (ConvertTo-QuotedArgument $logPath)
    )

    Write-Host "Unity EditModeテストを実行します: $resolvedUnityPath"
    $unityProcess = Start-Process `
        -FilePath $resolvedUnityPath `
        -ArgumentList $unityArguments `
        -WindowStyle Hidden `
        -PassThru

    if (-not $unityProcess.WaitForExit($TimeoutSeconds * 1000))
    {
        $unityProcess.Kill()
        $unityProcess.WaitForExit()
        throw "Unityテストが${TimeoutSeconds}秒以内に完了しませんでした。ログ: $logPath"
    }

    if ($unityProcess.ExitCode -ne 0)
    {
        throw "Unityが終了コード$($unityProcess.ExitCode)で失敗しました。ログ: $logPath"
    }

    if (-not (Test-Path -LiteralPath $resultPath -PathType Leaf))
    {
        throw "テスト結果が生成されませんでした。ログ: $logPath"
    }

    [xml]$testResult = Get-Content -Raw -LiteralPath $resultPath
    $testRun = $testResult.'test-run'
    $total = [int]$testRun.total
    $failed = [int]$testRun.failed
    $passed = [int]$testRun.passed

    if ($total -eq 0)
    {
        throw "テストが1件も検出されませんでした。結果: $resultPath"
    }

    if ($failed -gt 0 -or $testRun.result -ne 'Passed')
    {
        throw "EditModeテストが失敗しました（成功: $passed、失敗: $failed）。結果: $resultPath"
    }

    Write-Host "EditModeテスト成功（成功: $passed、失敗: $failed）"
    Write-Host "結果: $resultPath"
    exit 0
}
catch
{
    Write-Error $_.Exception.Message
    exit 1
}
