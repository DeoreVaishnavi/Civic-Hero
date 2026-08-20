$ProjectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\.."))
$DeploymentRoot = Join-Path $ProjectRoot "deployment"
$EnvFile = Join-Path $DeploymentRoot ".env.aws-rds"

Push-Location $DeploymentRoot
try {
    docker compose `
      --env-file $EnvFile `
      -f docker-compose.yml `
      -f docker-compose.aws-rds.override.yml `
      logs -f --tail 200 backend reverse-proxy
}
finally {
    Pop-Location
}
