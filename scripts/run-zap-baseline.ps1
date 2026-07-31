param(
    [string]$TargetUrl = "http://host.docker.internal:5180",
    [string]$ProjectRoot = "C:\Users\vaish\Music\CivicHeroSolution"
)

$ErrorActionPreference = "Stop"
$rules = Join-Path ([System.IO.Path]::GetFullPath($ProjectRoot)) "tests\security\zap-baseline.conf"
$reports = Join-Path ([System.IO.Path]::GetFullPath($ProjectRoot)) "tests\reports\zap"
New-Item -ItemType Directory -Path $reports -Force | Out-Null

docker run --rm -t `
  -v "${rules}:/zap/wrk/zap-baseline.conf:ro" `
  -v "${reports}:/zap/wrk/reports" `
  ghcr.io/zaproxy/zaproxy:stable `
  zap-baseline.py -t $TargetUrl -c zap-baseline.conf -r reports/zap-report.html -J reports/zap-report.json

if ($LASTEXITCODE -ne 0) { throw "OWASP ZAP baseline reported release-blocking findings." }
Write-Host "ZAP baseline passed. Reports: $reports" -ForegroundColor Green
