param(
    [string]$ProjectRoot = "",
    [string]$Tag = "phase15-local"
)

$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = Join-Path $PSScriptRoot "..\.."
}
$ProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot)
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) { throw "Docker is not installed." }

Push-Location $ProjectRoot
try {
    docker build -f deployment/backend/Dockerfile -t "civichero-backend:$Tag" .
    if ($LASTEXITCODE -ne 0) { throw "Backend container build failed." }
    docker build -f deployment/frontend/Dockerfile -t "civichero-frontend:$Tag" .
    if ($LASTEXITCODE -ne 0) { throw "Frontend container build failed." }
}
finally { Pop-Location }

Write-Host "Built civichero-backend:$Tag and civichero-frontend:$Tag" -ForegroundColor Green
