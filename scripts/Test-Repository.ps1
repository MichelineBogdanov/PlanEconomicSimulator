[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$required = @(
    'AGENTS.md', 'SPEC.md', 'DESIGN.md', 'ProjectSettings/ProjectVersion.txt',
    'Packages/manifest.json', 'Packages/packages-lock.json',
    'Assets/Game/Scenes/Bootstrap.unity', 'docs/design/tokens.json'
)
foreach ($path in $required) {
    if (!(Test-Path -LiteralPath (Join-Path $projectRoot $path))) { throw "Required file missing: $path." }
}
foreach ($path in @('Packages/manifest.json', 'Packages/packages-lock.json', 'docs/design/tokens.json', 'docs/design/asset-register.json')) {
    Get-Content (Join-Path $projectRoot $path) -Raw | ConvertFrom-Json | Out-Null
}
$version = Get-Content (Join-Path $projectRoot 'ProjectSettings/ProjectVersion.txt') -Raw
if ($version -notmatch '(?m)^m_EditorVersion: 6000\.6\.0f1\s*$') { throw 'Unexpected Unity version.' }
$guids = @{}
foreach ($asset in Get-ChildItem (Join-Path $projectRoot 'Assets') -Recurse -Force) {
    if ($asset.Name.EndsWith('.meta')) { continue }
    $metaPath = "$($asset.FullName).meta"
    if (!(Test-Path -LiteralPath $metaPath)) { throw "Missing .meta for $($asset.FullName)." }
    $guid = [regex]::Match((Get-Content $metaPath -Raw), '(?m)^guid: ([a-f0-9]{32})\s*$').Groups[1].Value
    if (!$guid -or $guids.ContainsKey($guid)) { throw "Invalid or duplicate Unity GUID: $metaPath." }
    $guids[$guid] = $metaPath
}
Write-Output "Repository configuration and $($guids.Count) Unity asset GUIDs verified."
