param(
    [string]$ProjectRoot = "C:\Users\vaish\Music\CivicHeroSolution",
    [switch]$SkipE2E,
    [switch]$SkipApi,
    [switch]$SkipPerformance
)

$ErrorActionPreference = "Stop"
$ProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot)
$BackendTests = Join-Path $ProjectRoot "backend\CivicHero.Backend.Tests\CivicHero.Backend.Tests.csproj"
$Frontend = Join-Path $ProjectRoot "frontend\civichero-web"
$ReportRoot = Join-Path $ProjectRoot ("tests\reports\phase14-" + (Get-Date -Format "yyyyMMdd-HHmmss"))
New-Item -ItemType Directory -Path $ReportRoot -Force | Out-Null

function Run-Step([string]$Name, [scriptblock]$Action) {
    Write-Host ""; Write-Host "==> $Name" -ForegroundColor Cyan
    & $Action
    if ($LASTEXITCODE -ne 0) { throw "$Name failed with exit code $LASTEXITCODE." }
}

Run-Step "Backend restore" { dotnet restore $BackendTests }
Run-Step "Backend tests and coverage" { dotnet test $BackendTests --configuration Release --no-restore --collect:"XPlat Code Coverage" --results-directory (Join-Path $ReportRoot "backend") --logger "trx;LogFileName=backend-tests.trx" }

Push-Location $Frontend
try {
    Run-Step "Frontend dependency install" { npm install }
    Run-Step "Frontend production build" { npm run build }
    Run-Step "Frontend tests and coverage" { npm run test:coverage }
    Copy-Item (Join-Path $Frontend "coverage") (Join-Path $ReportRoot "frontend-coverage") -Recurse -Force -ErrorAction SilentlyContinue

    if (-not $SkipE2E) {
        Run-Step "Playwright Chromium install" { npx playwright install chromium }
        Run-Step "Playwright public smoke tests" { npx playwright test --project=chromium }
        Copy-Item (Join-Path $Frontend "playwright-report") (Join-Path $ReportRoot "playwright-report") -Recurse -Force -ErrorAction SilentlyContinue
    }
}
finally { Pop-Location }

if (-not $SkipApi) {
    $newman = Get-Command newman -ErrorAction SilentlyContinue
    if ($newman) {
        Run-Step "Postman/Newman API smoke tests" { newman run (Join-Path $ProjectRoot "docs\Postman_Collection.json") --reporters cli,junit --reporter-junit-export (Join-Path $ReportRoot "newman.xml") }
    } else {
        Write-Host "Newman not installed; API smoke step skipped. Install with: npm install -g newman" -ForegroundColor Yellow
    }
}

if (-not $SkipPerformance) {
    $k6 = Get-Command k6 -ErrorAction SilentlyContinue
    if ($k6) {
        Run-Step "k6 health performance smoke" { k6 run -e CIVICHERO_API_URL=http://localhost:5180 (Join-Path $ProjectRoot "tests\performance\health-smoke.js") }
    } else {
        Write-Host "k6 not installed; performance step skipped. The Docker command is documented in Phase 14." -ForegroundColor Yellow
    }
}

@{
    generatedAt = (Get-Date).ToString("o")
    projectRoot = $ProjectRoot
    reportDirectory = $ReportRoot
    note = "A successful script exit means all non-skipped mandatory quality gates passed. Optional tools are explicitly reported as skipped."
} | ConvertTo-Json | Set-Content (Join-Path $ReportRoot "quality-summary.json") -Encoding UTF8

Write-Host ""; Write-Host "PHASE 14 QUALITY GATES COMPLETED" -ForegroundColor Green
Write-Host "Reports: $ReportRoot" -ForegroundColor Green
