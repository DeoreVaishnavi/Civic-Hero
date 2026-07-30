param(
    [string]$ProjectRoot = "C:\Users\vaish\Music\CivicHeroSolution",
    [string]$BaseUrl = "http://localhost:8088"
)
$ErrorActionPreference = "Stop"
$ProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot)
$frontend = Join-Path $ProjectRoot "frontend\civichero-web"
$required = @(
    "CIVICHERO_SUPERVISOR_EMAIL", "CIVICHERO_SUPERVISOR_PASSWORD",
    "CIVICHERO_OFFICER_EMAIL", "CIVICHERO_OFFICER_PASSWORD",
    "CIVICHERO_E2E_DEPARTMENT_ID", "CIVICHERO_E2E_WARD_ID", "CIVICHERO_E2E_OFFICER_ID"
)
$missing = $required | Where-Object { [string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($_)) }
if ($missing.Count -gt 0) { throw "Missing E2E environment variables: $($missing -join ', ')" }
$env:CIVICHERO_BASE_URL = $BaseUrl.TrimEnd('/')
$env:CIVICHERO_WEB_URL = $BaseUrl.TrimEnd('/')
$env:CIVICHERO_SKIP_WEB_SERVER = "1"
$report = Join-Path $ProjectRoot "artifacts\phase18\playwright"
New-Item -ItemType Directory -Path $report -Force | Out-Null
Push-Location $frontend
try {
    npx playwright install chromium
    if ($LASTEXITCODE -ne 0) { throw "Playwright Chromium installation failed." }
    npx playwright test e2e/critical-complaint-flow.spec.js --project=chromium --reporter=list,html
    if ($LASTEXITCODE -ne 0) { throw "Critical complaint E2E test failed." }
} finally { Pop-Location }
Write-Host "Critical complaint E2E passed." -ForegroundColor Green
