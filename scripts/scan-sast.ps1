param([ValidateSet('before','after')][string]$Label = 'before')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$image = 'semgrep/semgrep:1.175.0@sha256:b94b53d02fd4a022f9eac4e2af1380f5c3c4c21400e79d3336bdff1d1db5e796'
$outDir = Join-Path $root 'reports/local'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$report = Join-Path $outDir "semgrep-$Label.json"
if (Test-Path $report) { Remove-Item -LiteralPath $report }
& docker run --rm --network none --mount "type=bind,source=$root,target=/src" --workdir /src `
    --env SEMGREP_SEND_METRICS=off $image semgrep scan --config semgrep.yml `
    --metrics=off --disable-version-check --no-git-ignore --json `
    --output "reports/local/semgrep-$Label.json" src
$code = $LASTEXITCODE
if ($code -ne 0) { throw "Semgrep did not complete: exit $code. Read its output." }
if (-not (Test-Path $report)) { throw 'Semgrep report was not created.' }
$data = Get-Content -Raw -LiteralPath $report | ConvertFrom-Json
if (@($data.errors).Count -gt 0) { throw "Semgrep reported scan errors. Inspect $report" }
Write-Host "Semgrep completed; findings: $(@($data.results).Count). Report: $report"
Write-Host 'Exit 0 means the scan completed. It does not mean the code has no defects.'
$data.results | ForEach-Object { Write-Host "$($_.check_id)  $($_.path):$($_.start.line)" }
