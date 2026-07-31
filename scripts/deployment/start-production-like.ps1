param(
    [string]$ProjectRoot = "",
    [switch]$WithMonitoring,
    [switch]$NoBrowser
)

$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = Join-Path $PSScriptRoot "..\.."
}
$ProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot)
$EnvFile = Join-Path $ProjectRoot "deployment\.env.local"
$ComposeFile = Join-Path $ProjectRoot "deployment\docker-compose.yml"
$MonitoringComposeFile = Join-Path $ProjectRoot "deployment\docker-compose.monitoring.yml"
$Preflight = Join-Path $ProjectRoot "scripts\deployment\preflight.ps1"
$Verify = Join-Path $ProjectRoot "scripts\deployment\verify-deployment.ps1"

& powershell -ExecutionPolicy Bypass -File $Preflight -ProjectRoot $ProjectRoot -Environment Local
if ($LASTEXITCODE -ne 0) { throw "Deployment preflight failed." }

Push-Location $ProjectRoot
try {
    $arguments = @("compose", "--env-file", $EnvFile, "-f", $ComposeFile)
    if ($WithMonitoring) { $arguments += @("-f", $MonitoringComposeFile) }
    $arguments += @("up", "-d", "--build", "--remove-orphans")
    & docker @arguments
    if ($LASTEXITCODE -ne 0) { throw "Production-like Docker deployment failed." }
}
finally { Pop-Location }

$envValues = @{}
Get-Content $EnvFile | Where-Object { $_ -match '^[^#=]+=' } | ForEach-Object {
    $pair = $_ -split '=', 2
    $envValues[$pair[0].Trim()] = $pair[1].Trim()
}
$port = if ($envValues.ContainsKey('PUBLIC_HTTP_PORT')) { $envValues['PUBLIC_HTTP_PORT'] } else { '5173' }
$baseUrl = "http://localhost:$port"

& powershell -ExecutionPolicy Bypass -File $Verify -BaseUrl $baseUrl
if ($LASTEXITCODE -ne 0) { throw "Deployment started but verification failed." }

Write-Host "CivicHero production-like environment is running at $baseUrl" -ForegroundColor Green
if (-not $NoBrowser) { Start-Process $baseUrl }
