[CmdletBinding()]
param([Parameter(Mandatory)][string]$Path)

$ErrorActionPreference = 'Stop'
$files = @(Get-ChildItem -LiteralPath $Path -Filter '*.xml' -Recurse -File)
if ($files.Count -eq 0) { throw "No NUnit XML results found in $Path." }

$executed = 0
foreach ($file in $files) {
    [xml]$report = Get-Content -LiteralPath $file.FullName -Raw
    $run = $report.SelectSingleNode('/test-run')
    if ($null -eq $run) { throw "Unsupported NUnit result format: $($file.FullName)." }
    $passed = [int]$run.GetAttribute('passed')
    if ($run.GetAttribute('result') -ne 'Passed' -or $passed -le 0 -or [int]$run.GetAttribute('failed') -gt 0) {
        throw "Tests did not pass: $($file.Name), result=$($run.GetAttribute('result')), passed=$passed."
    }
    $executed += $passed
}
Write-Output "NUnit results verified: $executed passed tests in $($files.Count) report(s)."
