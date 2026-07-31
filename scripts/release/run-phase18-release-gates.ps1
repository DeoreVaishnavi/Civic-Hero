param(
    [string]$ProjectRoot = "C:\Users\vaish\Music\CivicHeroSolution",
    [string]$ReleaseVersion = "v1.0.0-rc.1",
    [string]$BaseUrl = "http://localhost:5173",
    [switch]$SkipDockerDeployment,
    [switch]$SkipSecurity,
    [switch]$SkipPerformance,
    [switch]$SkipCriticalE2E,
    [switch]$AllowDirtyWorkingTree
)
$ErrorActionPreference = "Stop"
$ProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot)
$backend = Join-Path $ProjectRoot "backend\CivicHero.Backend"
$tests = Join-Path $ProjectRoot "backend\CivicHero.Backend.Tests\CivicHero.Backend.Tests.csproj"
$frontend = Join-Path $ProjectRoot "frontend\civichero-web"
$artifactRoot = Join-Path $ProjectRoot "artifacts\phase18"
New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
$gates = [System.Collections.Generic.List[object]]::new()

function Add-Gate([string]$Key,[string]$Name,[bool]$Required,[scriptblock]$Action,[string]$EvidencePath,[bool]$Skip=$false) {
    $watch = [Diagnostics.Stopwatch]::StartNew(); $status = "Passed"; $detail = "Completed successfully."
    if ($Skip) { $status = "Skipped"; $detail = "Skipped by command-line switch. Required skipped gates block release approval." }
    else { try { & $Action } catch { $status = "Failed"; $detail = $_.Exception.Message } }
    $watch.Stop(); $gates.Add([pscustomobject]@{ key=$Key; name=$Name; status=$status; required=$Required; detail=$detail; evidencePath=$EvidencePath; durationSeconds=[Math]::Round($watch.Elapsed.TotalSeconds,2) })
    $colour = if ($status -eq "Passed") { "Green" } elseif ($status -eq "Failed") { "Red" } else { "Yellow" }
    Write-Host "[$status] $Name - $detail" -ForegroundColor $colour
}
function Require-Command([string]$Name) { if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) { throw "$Name is not installed or is not available on PATH." } }

Add-Gate "source" "Final source and placeholder audit" $true { & (Join-Path $ProjectRoot "scripts\release\audit-final-source.ps1") -ProjectRoot $ProjectRoot } "artifacts/phase18/source-audit/final-source-audit.json"
Add-Gate "git" "Clean Git working tree" $true {
    Require-Command git; Push-Location $ProjectRoot; try { $script:gitCommit = (git rev-parse HEAD).Trim(); if ($LASTEXITCODE -ne 0) { throw "Git commit could not be resolved." }; $dirty = git status --porcelain; if ($dirty -and -not $AllowDirtyWorkingTree) { throw "Git working tree is not clean. Commit or stash changes." } } finally { Pop-Location }
} "Git HEAD and status"
Add-Gate "backend" "Backend Release build and xUnit coverage" $true {
    Require-Command dotnet; $env:CIVICHERO_RUN_TESTCONTAINERS="1"; dotnet restore $tests; if ($LASTEXITCODE -ne 0) { throw "Backend restore failed." }; dotnet build $tests --configuration Release --no-restore; if ($LASTEXITCODE -ne 0) { throw "Backend build failed." }; dotnet test $tests --configuration Release --no-build --collect:"XPlat Code Coverage" --logger "trx;LogFileName=phase18-backend.trx" --results-directory (Join-Path $artifactRoot "backend-tests"); if ($LASTEXITCODE -ne 0) { throw "Backend tests failed." }
} "artifacts/phase18/backend-tests"
Add-Gate "frontend" "Frontend production build and Vitest coverage" $true {
    Require-Command npm; Push-Location $frontend; try { if (Test-Path "package-lock.json") { npm ci } else { npm install }; if ($LASTEXITCODE -ne 0) { throw "Frontend dependency installation failed." }; npm run build; if ($LASTEXITCODE -ne 0) { throw "Frontend build failed." }; npm run test:coverage; if ($LASTEXITCODE -ne 0) { throw "Frontend tests failed." } } finally { Pop-Location }
} "frontend/civichero-web/coverage"
Add-Gate "migrations" "Clean MySQL 8 migration test" $true { & (Join-Path $ProjectRoot "scripts\release\verify-clean-migrations.ps1") -ProjectRoot $ProjectRoot } "artifacts/phase18/migrations/clean-migration.json"
Add-Gate "docker" "Production-like Docker deployment" $true {
    Require-Command docker; Push-Location $ProjectRoot; try { docker compose --env-file ".\deployment\.env.local" -f ".\deployment\docker-compose.yml" -f ".\deployment\docker-compose.monitoring.yml" config --quiet; if ($LASTEXITCODE -ne 0) { throw "Compose validation failed." }; docker compose --env-file ".\deployment\.env.local" -f ".\deployment\docker-compose.yml" -f ".\deployment\docker-compose.monitoring.yml" build; if ($LASTEXITCODE -ne 0) { throw "Container build failed." }; docker compose --env-file ".\deployment\.env.local" -f ".\deployment\docker-compose.yml" -f ".\deployment\docker-compose.monitoring.yml" up -d; if ($LASTEXITCODE -ne 0) { throw "Docker deployment failed." }; & ".\scripts\deployment\verify-deployment.ps1" -BaseUrl $BaseUrl } finally { Pop-Location }
} "Docker Compose and deployment verification" $SkipDockerDeployment
Add-Gate "e2e" "Critical complaint E2E journey" $true { & (Join-Path $ProjectRoot "scripts\release\run-critical-e2e.ps1") -ProjectRoot $ProjectRoot -BaseUrl $BaseUrl } "frontend/civichero-web/playwright-report" $SkipCriticalE2E
Add-Gate "newman" "Postman/Newman API regression" $true {
    Require-Command newman; newman run (Join-Path $ProjectRoot "tests\api\CivicHero-Release-Smoke.postman_collection.json") --env-var "baseUrl=$BaseUrl" --reporters cli,json --reporter-json-export (Join-Path $artifactRoot "newman.json"); if ($LASTEXITCODE -ne 0) { throw "Newman regression failed." }
} "artifacts/phase18/newman.json"
Add-Gate "performance" "k6 release performance baseline" $true {
    Require-Command k6; k6 run -e "BASE_URL=$BaseUrl" --summary-export (Join-Path $artifactRoot "k6-summary.json") (Join-Path $ProjectRoot "tests\performance\release-baseline.js"); if ($LASTEXITCODE -ne 0) { throw "k6 thresholds failed." }
} "artifacts/phase18/k6-summary.json" $SkipPerformance
Add-Gate "security" "OWASP ZAP baseline" $true {
    Require-Command docker; $zapTarget = $BaseUrl.Replace("localhost", "host.docker.internal").Replace("127.0.0.1", "host.docker.internal"); & (Join-Path $ProjectRoot "scripts\run-zap-baseline.ps1") -ProjectRoot $ProjectRoot -TargetUrl $zapTarget; if ($LASTEXITCODE -ne 0) { throw "ZAP baseline failed." }
} "TestResults/security" $SkipSecurity
Add-Gate "monitoring" "Application and monitoring endpoints" $true { & (Join-Path $ProjectRoot "scripts\release\verify-monitoring.ps1") -ProjectRoot $ProjectRoot -ApplicationUrl $BaseUrl } "artifacts/phase18/monitoring/monitoring-verification.json"
Add-Gate "backup" "Backup and restore rehearsal evidence" $true { & (Join-Path $ProjectRoot "scripts\release\validate-backup-restore.ps1") -ProjectRoot $ProjectRoot } "artifacts/phase18/backup-restore-evidence.json"
Add-Gate "uat" "Signed UAT evidence" $true { & (Join-Path $ProjectRoot "scripts\release\validate-uat-signoff.ps1") -ProjectRoot $ProjectRoot } "tests/uat/phase18-uat-signoff.json"

if (-not $SkipDockerDeployment -and (Get-Command docker -ErrorAction SilentlyContinue)) { Push-Location $ProjectRoot; try { docker compose --env-file ".\deployment\.env.local" -f ".\deployment\docker-compose.yml" -f ".\deployment\docker-compose.monitoring.yml" down } finally { Pop-Location } }
$required = @($gates | Where-Object required)
$approved = $required.Count -gt 0 -and @($required | Where-Object status -ne "Passed").Count -eq 0
$evidence = [ordered]@{
    schemaVersion=1; phase=18; project="CivicHero"; releaseVersion=$ReleaseVersion; gitCommit=$(if ([string]::IsNullOrWhiteSpace($script:gitCommit)) { "unknown" } else { $script:gitCommit }); generatedAtUtc=(Get-Date).ToUniversalTime().ToString('o'); approved=$approved;
    summary=[ordered]@{ total=$gates.Count; required=$required.Count; passed=@($gates|Where-Object status -eq "Passed").Count; failed=@($gates|Where-Object status -eq "Failed").Count; skipped=@($gates|Where-Object status -eq "Skipped").Count };
    environment=[ordered]@{ computer=$env:COMPUTERNAME; user=$env:USERNAME; dotnet=$(if(Get-Command dotnet -ErrorAction SilentlyContinue){dotnet --version}else{"missing"}); node=$(if(Get-Command node -ErrorAction SilentlyContinue){node --version}else{"missing"}); docker=$(if(Get-Command docker -ErrorAction SilentlyContinue){docker --version}else{"missing"}) };
    gates=$gates
}
$evidence | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $artifactRoot "release-evidence.json") -Encoding UTF8
$lines = @("# CivicHero Phase 18 Release Gate Summary", "", "Release: $ReleaseVersion", "Approved: $approved", "Generated: $($evidence.generatedAtUtc)", "", "| Gate | Status | Required | Detail |", "|---|---|---:|---|")
foreach ($gate in $gates) { $lines += "| $($gate.name) | $($gate.status) | $($gate.required) | $($gate.detail.Replace('|','/')) |" }
$lines | Set-Content (Join-Path $artifactRoot "release-gate-summary.md") -Encoding UTF8
if (-not $approved) { throw "Phase 18 release is BLOCKED. Review artifacts/phase18/release-evidence.json." }
Write-Host "All required Phase 18 gates passed. Release evidence is approved." -ForegroundColor Green
