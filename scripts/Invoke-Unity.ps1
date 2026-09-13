[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('Configure', 'EditMode', 'PlayMode', 'Build')][string]$Task,
    [string]$UnityPath = $env:UNITY_EDITOR_PATH,
    [ValidateRange(30, 3600)][int]$TimeoutSeconds = 900
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$versionFile = Join-Path $projectRoot 'ProjectSettings/ProjectVersion.txt'
$version = [regex]::Match((Get-Content $versionFile -Raw), '(?m)^m_EditorVersion: (\S+)').Groups[1].Value
if ([string]::IsNullOrWhiteSpace($version)) { throw 'The Unity editor version is not pinned.' }
if (!$UnityPath) { $UnityPath = "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe" }
if (!(Test-Path -LiteralPath $UnityPath -PathType Leaf)) { throw "Unity editor not found: $UnityPath." }
$installedVersion = (Get-Item -LiteralPath $UnityPath).VersionInfo.ProductVersion.Split('_')[0]
if ($installedVersion -ne $version) { throw "Expected Unity $version, found $installedVersion." }

$lockPath = Join-Path $projectRoot 'Temp/UnityLockfile'
if (Test-Path -LiteralPath $lockPath) {
    try { $handle = [IO.File]::Open($lockPath, 'Open', 'ReadWrite', 'None'); $handle.Dispose() }
    catch { throw 'The project is open in another Unity process. Close that editor before running CLI tasks.' }
}

$runDirectory = Join-Path $projectRoot "TestResults/$Task/$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $runDirectory -Force | Out-Null
$logPath = Join-Path $runDirectory 'Editor.log'
$arguments = @('-batchmode', '-projectPath', "`"$projectRoot`"", '-logFile', "`"$logPath`"")
if ($Task -in @('EditMode', 'PlayMode')) {
    $resultPath = Join-Path $runDirectory 'results.xml'
    $arguments += @('-runTests', '-testPlatform', $Task, '-testResults', "`"$resultPath`"")
    if ($Task -eq 'EditMode') { $arguments += '-nographics' }
} else {
    $method = if ($Task -eq 'Configure') { 'Configure' } else { 'BuildWindows' }
    $arguments += @('-nographics', '-executeMethod', "PlanEconomicSimulator.Editor.ProjectBuild.$method", '-quit')
}

$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
if (!$process.WaitForExit($TimeoutSeconds * 1000)) {
    $process.Kill()
    throw "Unity $Task timed out. Log: $logPath."
}
if ($process.ExitCode -ne 0) { throw "Unity $Task exited with code $($process.ExitCode). Log: $logPath." }
if ($Task -in @('EditMode', 'PlayMode')) { & "$PSScriptRoot/Test-Results.ps1" -Path $runDirectory }
if ($Task -eq 'Build' -and !(Test-Path "$projectRoot/Builds/Windows/PlanEconomicSimulator.exe")) {
    throw "Unity exited without creating the Windows player. Log: $logPath."
}
Write-Output "Unity $Task completed. Log: $logPath"
