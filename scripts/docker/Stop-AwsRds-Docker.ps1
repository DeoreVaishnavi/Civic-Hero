$ErrorActionPreference = "Stop"
$ProjectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\.."))
$DeploymentRoot = Join-Path $ProjectRoot "deployment"
$EnvFile = Join-Path $DeploymentRoot ".env.aws-rds"
$TunnelPidFile = Join-Path $DeploymentRoot ".aws-rds-tunnel.pid"

Push-Location $DeploymentRoot
try {
    docker compose `
      --env-file $EnvFile `
      -f docker-compose.yml `
      -f docker-compose.aws-rds.override.yml `
      down --remove-orphans
}
finally {
    Pop-Location
}

if (Test-Path -LiteralPath $TunnelPidFile) {
    $pidValue = [int](Get-Content -LiteralPath $TunnelPidFile -Raw)
    Stop-Process -Id $pidValue -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $TunnelPidFile -Force -ErrorAction SilentlyContinue
}
