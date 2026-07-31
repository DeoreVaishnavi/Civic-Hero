param(
    [string]$ProjectRoot = "",
    [ValidateSet("Local", "Production")]
    [string]$Environment = "Local"
)

$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = Join-Path $PSScriptRoot "..\.."
}
$ProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot)
$Deployment = Join-Path $ProjectRoot "deployment"
$EnvFile = if ($Environment -eq "Production") { Join-Path $Deployment ".env.production.local" } else { Join-Path $Deployment ".env.local" }
$ComposeFile = if ($Environment -eq "Production") { Join-Path $Deployment "docker-compose.prod.yml" } else { Join-Path $Deployment "docker-compose.yml" }
$SecretDirectory = Join-Path $Deployment ".secrets"

function Require-Command([string]$Name) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) { throw "$Name is not installed or not available in PATH." }
}

function Require-File([string]$Path, [string]$Description) {
    if (-not (Test-Path $Path)) { throw "$Description is missing: $Path" }
}

Write-Host "CivicHero Phase 15 deployment preflight" -ForegroundColor Cyan
Require-Command docker
& docker info *> $null
if ($LASTEXITCODE -ne 0) { throw "Docker Desktop or Docker Engine is not running." }
& docker compose version *> $null
if ($LASTEXITCODE -ne 0) { throw "Docker Compose v2 is required." }

Require-File $EnvFile "Environment file"
Require-File $ComposeFile "Compose file"
foreach ($name in @("rds_connection.txt", "jwt_secret.txt", "aws_access_key.txt", "aws_secret_key.txt", "ai_api_key.txt")) {
    Require-File (Join-Path $SecretDirectory $name) "Secret file"
}

$envText = Get-Content $EnvFile -Raw
if ($envText -match 'replace-|example\.com|CHANGE-ME|YOUR_') {
    throw "The environment file still contains placeholder values: $EnvFile"
}

$envValues = @{}
Get-Content -LiteralPath $EnvFile |
    Where-Object { $_ -match '^[^#=]+=' } |
    ForEach-Object {
        $pair = $_ -split '=', 2
        $envValues[$pair[0].Trim()] = $pair[1].Trim()
    }

$publicPort = if ($envValues.ContainsKey('PUBLIC_HTTP_PORT')) {
    [int]$envValues['PUBLIC_HTTP_PORT']
}
else {
    5173
}

Write-Host "Public website port: $publicPort" -ForegroundColor Cyan
$listener = Get-NetTCPConnection -LocalPort $publicPort -State Listen -ErrorAction SilentlyContinue
if ($listener) {
    Write-Warning "Port $publicPort is already in use. Do not run the Vite dev server and the Docker website on the same port at the same time. Stop the existing listener or change PUBLIC_HTTP_PORT."
}

$jwt = Get-Content (Join-Path $SecretDirectory "jwt_secret.txt") -Raw
if ($jwt.Trim().Length -lt 32) { throw "JWT secret must contain at least 32 characters." }
$rds = Get-Content (Join-Path $SecretDirectory "rds_connection.txt") -Raw
if ([string]::IsNullOrWhiteSpace($rds) -or $rds -match 'YOUR_|example') { throw "A real AWS RDS connection string is required." }

if ($Environment -eq "Production") {
    Require-File (Join-Path $Deployment "nginx\certs\fullchain.pem") "TLS full chain"
    Require-File (Join-Path $Deployment "nginx\certs\privkey.pem") "TLS private key"
    Require-File (Join-Path $SecretDirectory "grafana_admin_password.txt") "Grafana password secret"
}

Push-Location $ProjectRoot
try {
    & docker compose --env-file $EnvFile -f $ComposeFile config --quiet
    if ($LASTEXITCODE -ne 0) { throw "Docker Compose configuration validation failed." }
}
finally { Pop-Location }

Write-Host "Deployment preflight passed for $Environment." -ForegroundColor Green
