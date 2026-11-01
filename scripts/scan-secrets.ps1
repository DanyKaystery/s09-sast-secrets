param([ValidateSet('history','tree')][string]$Mode = 'history')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$history = Join-Path (Split-Path $root -Parent) 's09-secret-history'
if (-not (Test-Path (Join-Path $history '.git'))) {
    throw 'First clone fixtures/secret-history.bundle into ../s09-secret-history, as described in README.'
}
$image = 'ghcr.io/gitleaks/gitleaks:v8.30.1@sha256:c00b6bd0aeb3071cbcb79009cb16a60dd9e0a7c60e2be9ab65d25e6bc8abbb7f'
$outDir = Join-Path $root 'reports/local'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$report = Join-Path $outDir "gitleaks-$Mode.json"
if (Test-Path $report) { Remove-Item -LiteralPath $report }
$scanArgs = @('git', '/history', '--log-opts=--all')
if ($Mode -eq 'tree') { $scanArgs = @('dir', '/history') }
& docker run --rm --network none --mount "type=bind,source=$history,target=/history,readonly" `
    --mount "type=bind,source=$root,target=/lab" $image @scanArgs `
    --config /lab/.gitleaks-training.toml --redact --no-banner `
    --report-format json --report-path "/lab/reports/local/gitleaks-$Mode.json"
$code = $LASTEXITCODE
if (-not (Test-Path $report)) { throw "Gitleaks produced no report; exit $code. Read its output." }
# Normalize JSON [] / one result / many results in Windows PowerShell 5.1.
$data = @(Get-Content -Raw -LiteralPath $report | ConvertFrom-Json)
$data = @($data | Where-Object { $null -ne $_ })
$count = $data.Count
if (($code -eq 1 -and $count -gt 0) -or ($code -eq 0 -and $count -eq 0)) {
    Write-Host "Gitleaks completed; findings: $count; scanner exit: $code. Report: $report"
    foreach ($finding in $data) { Write-Host "$($finding.RuleID)  $($finding.File):$($finding.StartLine)  commit $($finding.Commit)" }
} else { throw "Unexpected Gitleaks result: exit $code, findings $count. Read its output." }
