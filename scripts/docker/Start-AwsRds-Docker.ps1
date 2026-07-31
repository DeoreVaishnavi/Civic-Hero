[CmdletBinding()]
param(
    [switch]$NoBuild,
    [switch]$NoBrowser
)

$ErrorActionPreference = "Stop"
$ProjectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\.."))
$DeploymentRoot = Join-Path $ProjectRoot "deployment"
$BaseCompose = Join-Path $DeploymentRoot "docker-compose.yml"
$OverrideCompose = Join-Path $DeploymentRoot "docker-compose.aws-rds.override.yml"
$EnvFile = Join-Path $DeploymentRoot ".env.aws-rds"
$ConfigFile = Join-Path $DeploymentRoot ".aws-rds-docker.json"
$SecretsRoot = Join-Path $DeploymentRoot ".secrets"
$TunnelPidFile = Join-Path $DeploymentRoot ".aws-rds-tunnel.pid"

function Write-Step {
    param([string]$Text)
    Write-Host ""
    Write-Host "============================================================" -ForegroundColor Cyan
    Write-Host $Text -ForegroundColor Cyan
    Write-Host "============================================================" -ForegroundColor Cyan
}

function Wait-Port {
    param(
        [string]$HostName,
        [int]$Port,
        [int]$Seconds = 60
    )

    $deadline = (Get-Date).AddSeconds($Seconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-NetConnection -ComputerName $HostName -Port $Port -InformationLevel Quiet) {
            return $true
        }
        Start-Sleep -Seconds 3
    }

    return $false
}

Write-Step "1. Checking configuration and Docker"

foreach ($required in @(
    $BaseCompose,
    $OverrideCompose,
    $EnvFile,
    $ConfigFile,
    (Join-Path $SecretsRoot "rds_connection.txt"),
    (Join-Path $SecretsRoot "jwt_secret.txt"),
    (Join-Path $SecretsRoot "aws_access_key.txt"),
    (Join-Path $SecretsRoot "aws_secret_key.txt"),
    (Join-Path $SecretsRoot "ai_api_key.txt")
)) {
    if (-not (Test-Path -LiteralPath $required)) {
        throw "Required AWS RDS Docker file is missing: $required. Run configure-aws-rds.bat first."
    }
}

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw "Docker Desktop is not installed."
}

docker info *> $null
if ($LASTEXITCODE -ne 0) {
    throw "Docker Desktop is installed, but its engine is not running."
}

$config = Get-Content -LiteralPath $ConfigFile -Raw | ConvertFrom-Json

Write-Step "2. Checking the AWS RDS network path"

if ($config.Mode -eq "SsmTunnel") {
    if (-not (Get-Command aws -ErrorAction SilentlyContinue)) {
        throw "AWS CLI is required for SSM tunnel mode."
    }

    if (-not (Get-Command session-manager-plugin -ErrorAction SilentlyContinue)) {
        throw "AWS Session Manager plugin is required for SSM tunnel mode."
    }

    if (Test-Path -LiteralPath $TunnelPidFile) {
        $oldPid = [int](Get-Content -LiteralPath $TunnelPidFile -Raw)
        $oldProcess = Get-Process -Id $oldPid -ErrorAction SilentlyContinue
        if ($oldProcess) {
            Stop-Process -Id $oldPid -Force
        }
        Remove-Item -LiteralPath $TunnelPidFile -Force -ErrorAction SilentlyContinue
    }

    $parameterJson = @{
        host = @($config.RdsEndpoint)
        portNumber = @([string]$config.RdsPort)
        localPortNumber = @([string]$config.LocalTunnelPort)
    } | ConvertTo-Json -Compress

    $awsArguments = @(
        "ssm", "start-session",
        "--profile", $config.AwsProfile,
        "--region", $config.AwsRegion,
        "--target", $config.SsmTargetInstanceId,
        "--document-name", "AWS-StartPortForwardingSessionToRemoteHost",
        "--parameters", $parameterJson
    )

    $tunnelProcess = Start-Process `
        -FilePath "aws" `
        -ArgumentList $awsArguments `
        -PassThru `
        -WindowStyle Normal

    Set-Content `
        -LiteralPath $TunnelPidFile `
        -Value $tunnelProcess.Id `
        -Encoding ASCII `
        -NoNewline

    if (-not (Wait-Port -HostName "127.0.0.1" -Port $config.LocalTunnelPort -Seconds 90)) {
        throw "The SSM tunnel did not open local port $($config.LocalTunnelPort). Check IAM, EC2 SSM status, VPC routing and the RDS security group."
    }

    Write-Host "SSM tunnel is ready on localhost:$($config.LocalTunnelPort)." -ForegroundColor Green
}
else {
    $resolved = Resolve-DnsName $config.RdsEndpoint -ErrorAction SilentlyContinue
    if (-not $resolved) {
        throw "The RDS endpoint could not be resolved by DNS: $($config.RdsEndpoint)"
    }

    if (-not (Wait-Port -HostName $config.RdsEndpoint -Port $config.RdsPort -Seconds 20)) {
        throw @"
The RDS endpoint is not reachable from this laptop.

Check:
1. RDS Publicly accessible = Yes, OR connect the laptop to AWS Client VPN.
2. RDS security group inbound rule allows MySQL TCP $($config.RdsPort) from this laptop's public IP /32.
3. Network firewall or office VPN is not blocking the port.
4. The endpoint and port are correct.

For a private RDS instance, rerun configure-aws-rds.bat and choose SsmTunnel.
"@
    }

    Write-Host "RDS TCP connection is reachable." -ForegroundColor Green
}

Write-Step "3. Validating Docker Compose"

Push-Location $DeploymentRoot
try {
    $composeArguments = @(
        "compose",
        "--env-file", $EnvFile,
        "-f", $BaseCompose,
        "-f", $OverrideCompose
    )

    & docker @composeArguments "config" "--quiet"
    if ($LASTEXITCODE -ne 0) {
        throw "Docker Compose configuration is invalid."
    }

    Write-Step "4. Starting CivicHero with AWS RDS and S3"

    $upArguments = $composeArguments + @("up", "-d", "--remove-orphans")
    if (-not $NoBuild) {
        $upArguments += "--build"
    }

    & docker @upArguments
    if ($LASTEXITCODE -ne 0) {
        throw "Docker Compose startup failed."
    }

    Write-Step "5. Waiting for the backend"

    $deadline = (Get-Date).AddMinutes(6)
    $backendLive = $false

    while ((Get-Date) -lt $deadline) {
        try {
            $response = Invoke-WebRequest `
                -Uri "http://localhost:5180/health/live" `
                -UseBasicParsing `
                -TimeoutSec 5

            if ($response.StatusCode -eq 200) {
                $backendLive = $true
                break
            }
        }
        catch {
            Start-Sleep -Seconds 5
        }
    }

    if (-not $backendLive) {
        & docker @composeArguments "logs" "--tail" "150" "backend"
        throw "Backend did not become live. Review the backend logs above."
    }

    Write-Step "CivicHero AWS RDS profile is running"

    & docker @composeArguments "ps"

    Write-Host ""
    Write-Host "Website : http://localhost:8088" -ForegroundColor Green
    Write-Host "Swagger : http://localhost:5180/swagger" -ForegroundColor Green
    Write-Host "Health  : http://localhost:5180/health/ready" -ForegroundColor Green
    Write-Host ""
    Write-Host "Startup migrations are disabled. The existing AWS RDS table structure is not changed." -ForegroundColor Yellow

    try {
        $ready = Invoke-RestMethod `
            -Uri "http://localhost:5180/health/ready" `
            -Method Get `
            -TimeoutSec 15

        Write-Host ""
        Write-Host "Dependency health:" -ForegroundColor White
        $ready | ConvertTo-Json -Depth 10
    }
    catch {
        Write-Warning "The application is live, but one or more readiness dependencies are unhealthy. Run aws-rds-logs.bat."
    }

    if (-not $NoBrowser) {
        Start-Process "http://localhost:8088"
    }
}
finally {
    Pop-Location
}
