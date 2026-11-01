param([ValidateSet('before','after')][string]$Label = 'before')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$files = @('global.json', 'Directory.Build.props', 'NotesApi.slnx', 'semgrep.yml', '.gitleaks-training.toml')
$files += Get-ChildItem -Path (Join-Path $root 'src'), (Join-Path $root 'tests') -Recurse -File |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and ($_.Extension -in @('.cs','.csproj') -or $_.Name -eq 'packages.lock.json') } |
    ForEach-Object { $_.FullName.Substring($root.Length + 1).Replace('\','/') }
$hashes = [ordered]@{}
foreach ($file in ($files | Sort-Object)) {
    $hashes[$file] = (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $root $file)).Hash.ToLowerInvariant()
}
$commit = $null
$gitStatus = $null
Push-Location $root
try {
    # A source ZIP has no .git; that is a normal case, not a scanner error.
    if (Test-Path -LiteralPath (Join-Path $root '.git')) {
        try {
            $candidate = & git rev-parse HEAD 2>$null
            if ($LASTEXITCODE -eq 0) {
                $commit = ($candidate | Out-String).Trim()
                $gitStatus = @(& git status --short)
            }
        } catch { $commit = $null }
    }
} finally { Pop-Location }
$outDir = Join-Path $root 'reports/local'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
[ordered]@{ label=$Label; recorded_at_utc=[DateTime]::UtcNow.ToString('o'); commit=$commit; git_status=$gitStatus; sha256=$hashes } |
    ConvertTo-Json -Depth 5 | Set-Content -Encoding UTF8 -LiteralPath (Join-Path $outDir "source-$Label.json")
Write-Host "Recorded actual file hashes in reports/local/source-$Label.json"
