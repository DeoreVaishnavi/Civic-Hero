param([string]$ProjectRoot = "C:\Users\vaish\Music\CivicHeroSolution")
$ErrorActionPreference = "Stop"
$ProjectRoot = [IO.Path]::GetFullPath($ProjectRoot)
$Stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$OutputRoot = Join-Path $ProjectRoot "artifacts\phase16"
$Stage = Join-Path $OutputRoot "handover-$Stamp"
$Zip = Join-Path $OutputRoot "CivicHero-Handover-$Stamp.zip"
New-Item -ItemType Directory -Path $Stage -Force | Out-Null

foreach ($relative in @("docs", "database", "tests\uat", "deployment\.env.example", "deployment\.env.production.example", "README.md")) {
    $source = Join-Path $ProjectRoot $relative
    if (Test-Path $source) {
        $destination = Join-Path $Stage $relative
        New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
        Copy-Item $source $destination -Recurse -Force
    }
}

Get-ChildItem $Stage -Recurse -File | Where-Object {
    $_.Name -match '(?i)secret|password|credential|\.env\.(local|production)$'
} | Remove-Item -Force

Get-ChildItem $Stage -Recurse -File | ForEach-Object {
    [pscustomobject]@{ Path=$_.FullName.Substring($Stage.Length+1); SHA256=(Get-FileHash $_.FullName -Algorithm SHA256).Hash }
} | Export-Csv (Join-Path $Stage "HANDOVER-CHECKSUMS.csv") -NoTypeInformation

Compress-Archive -Path (Join-Path $Stage '*') -DestinationPath $Zip -Force
Write-Host "Handover bundle created: $Zip" -ForegroundColor Green
Write-Host "Secrets were intentionally excluded. Review the archive before sharing." -ForegroundColor Yellow
