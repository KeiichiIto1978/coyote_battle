[CmdletBinding()]
param(
    [string]$UnityPath,
    [string]$ProjectPath,
    [string]$OutputPath,
    [ValidateRange(1, 3600)]
    [int]$TimeoutSeconds = 1200
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-ProjectPath
{
    param([string]$RequestedPath)

    if ($RequestedPath)
    {
        return (Resolve-Path -LiteralPath $RequestedPath).Path
    }

    return (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
}

function Get-UnityPath
{
    param(
        [string]$RequestedPath,
        [string]$ResolvedProjectPath
    )

    if ($RequestedPath)
    {
        return (Resolve-Path -LiteralPath $RequestedPath).Path
    }

    $projectVersionPath = Join-Path $ResolvedProjectPath 'ProjectSettings\ProjectVersion.txt'
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

function Assert-AndroidModule
{
    param([string]$ResolvedUnityPath)

    $editorDataPath = Join-Path (Split-Path -Parent $ResolvedUnityPath) 'Data'
    $androidPlayerPath = Join-Path $editorDataPath 'PlaybackEngines\AndroidPlayer'
    foreach ($moduleName in @('SDK', 'NDK', 'OpenJDK'))
    {
        $modulePath = Join-Path $androidPlayerPath $moduleName
        if (-not (Test-Path -LiteralPath $modulePath -PathType Container))
        {
            throw "Required Android module was not found: $moduleName"
        }
    }
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
    $resolvedProjectPath = Get-ProjectPath -RequestedPath $ProjectPath
    if ($resolvedProjectPath -match '[^\x00-\x7F]')
    {
        throw 'Android Tools require an ASCII-only project path. Map the worktree to an ASCII drive path with subst and pass it through -ProjectPath.'
    }

    $resolvedUnityPath = Get-UnityPath -RequestedPath $UnityPath -ResolvedProjectPath $resolvedProjectPath
    Assert-AndroidModule -ResolvedUnityPath $resolvedUnityPath

    if (-not $OutputPath)
    {
        $OutputPath = Join-Path $resolvedProjectPath 'Builds\Android\CoyoteBattle-development.apk'
    }

    $resolvedOutputPath = [System.IO.Path]::GetFullPath($OutputPath)
    $buildEvidencePath = "$resolvedOutputPath.build.json"
    $logPath = Join-Path $resolvedProjectPath 'Logs\AndroidDevelopmentBuild.log'
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $logPath) | Out-Null

    if (Test-Path -LiteralPath $resolvedOutputPath -PathType Leaf)
    {
        Remove-Item -LiteralPath $resolvedOutputPath -Force
    }
    if (Test-Path -LiteralPath $buildEvidencePath -PathType Leaf)
    {
        Remove-Item -LiteralPath $buildEvidencePath -Force
    }

    $gitProjectPath = $resolvedProjectPath
    if ($gitProjectPath -match '^[A-Za-z]:\\$')
    {
        $gitProjectPath += '.'
    }
    $commitSha = (& git -C $gitProjectPath rev-parse HEAD 2>$null | Select-Object -First 1).Trim()
    if ($LASTEXITCODE -ne 0 -or $commitSha -notmatch '^[0-9a-fA-F]{40}$')
    {
        throw 'Could not determine the current commit SHA.'
    }
    & git -C $gitProjectPath diff-index --quiet HEAD --
    if ($LASTEXITCODE -eq 1)
    {
        throw 'Tracked changes exist. Commit them before building the Development APK.'
    }
    if ($LASTEXITCODE -ne 0)
    {
        throw 'Could not inspect the worktree state.'
    }

    $buildStartedAt = [DateTime]::UtcNow

    $arguments = @(
        '-batchmode',
        '-nographics',
        '-quit',
        '-projectPath', (ConvertTo-QuotedArgument -Value $resolvedProjectPath),
        '-buildTarget', 'Android',
        '-executeMethod', 'CoyoteBattle.Editor.AndroidDevelopmentBuilder.Build',
        '-androidOutputPath', (ConvertTo-QuotedArgument -Value $resolvedOutputPath),
        '-logFile', (ConvertTo-QuotedArgument -Value $logPath)
    )
    $process = Start-Process -FilePath $resolvedUnityPath -ArgumentList $arguments -PassThru
    if (-not $process.WaitForExit($TimeoutSeconds * 1000))
    {
        $process.Kill()
        throw "Android build timed out after $TimeoutSeconds seconds."
    }

    if ($process.ExitCode -ne 0)
    {
        throw "Unity Android build failed with exit code $($process.ExitCode). Log: $logPath"
    }

    $apk = Get-Item -LiteralPath $resolvedOutputPath -ErrorAction Stop
    if ($apk.Length -le 0)
    {
        throw "Generated APK is empty: $resolvedOutputPath"
    }

    if ($apk.LastWriteTimeUtc -lt $buildStartedAt)
    {
        throw "Generated APK was not updated by the current build: $resolvedOutputPath"
    }

    $apkHash = (Get-FileHash -LiteralPath $apk.FullName -Algorithm SHA256).Hash.ToUpperInvariant()
    [pscustomobject][ordered]@{
        BuiltAtUtc = [DateTime]::UtcNow.ToString('o')
        CommitSha = $commitSha.ToLowerInvariant()
        ApkSize = $apk.Length
        ApkSha256 = $apkHash
    } | ConvertTo-Json | Set-Content -LiteralPath $buildEvidencePath -Encoding UTF8

    Write-Output "Android Development APK succeeded ($($apk.Length) bytes)."
    Write-Output "Artifact: $resolvedOutputPath"
    Write-Output "Build evidence: $buildEvidencePath"
}
catch
{
    Write-Error $_
    exit 1
}
