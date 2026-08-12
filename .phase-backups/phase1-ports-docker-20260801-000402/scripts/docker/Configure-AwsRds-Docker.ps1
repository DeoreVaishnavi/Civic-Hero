[CmdletBinding()]
param(
    [ValidateSet("Direct", "SsmTunnel")]
    [string]$Mode
)

$ErrorActionPreference = "Stop"
$ProjectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\.."))
$DeploymentRoot = Join-Path $ProjectRoot "deployment"
$SecretsRoot = Join-Path $DeploymentRoot ".secrets"
$EnvFile = Join-Path $DeploymentRoot ".env.aws-rds"
$ConfigFile = Join-Path $DeploymentRoot ".aws-rds-docker.json"

function Read-Required {
    param(
        [string]$Prompt,
        [string]$DefaultValue = ""
    )

    $suffix = if ($DefaultValue) { " [$DefaultValue]" } else { "" }
    $value = Read-Host "$Prompt$suffix"

    if ([string]::IsNullOrWhiteSpace($value)) {
        $value = $DefaultValue
    }

    if ([string]::IsNullOrWhiteSpace($value)) {
        throw "$Prompt is required."
    }

    return $value.Trim()
}

function Convert-SecureToPlain {
    param([Security.SecureString]$SecureValue)

    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureValue)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    }
}

function Quote-ConnectionValue {
    param([string]$Value)
    return "'" + $Value.Replace("'", "''") + "'"
}

function New-RandomSecret {
    param([int]$ByteCount = 64)

    $bytes = New-Object byte[] $ByteCount
    $generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()

    try {
        $generator.GetBytes($bytes)
    }
    finally {
        $generator.Dispose()
    }

    return [Convert]::ToBase64String($bytes)
}

function Ensure-File {
    param(
        [string]$Path,
        [string]$Value = ""
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        Set-Content -LiteralPath $Path -Value $Value -Encoding UTF8 -NoNewline
    }
}

Write-Host ""
Write-Host "CivicHero Docker + AWS RDS configuration" -ForegroundColor Cyan
Write-Host "This does not run migrations or change table structure." -ForegroundColor Yellow
Write-Host ""

if (-not $Mode) {
    Write-Host "Choose connection mode:" -ForegroundColor White
    Write-Host "1. Direct     - RDS is publicly reachable or laptop is connected to AWS VPN"
    Write-Host "2. SsmTunnel  - RDS is private; connect through an SSM-managed EC2 instance"
    $choice = Read-Host "Enter 1 or 2"
    $Mode = if ($choice -eq "2") { "SsmTunnel" } else { "Direct" }
}

$RdsEndpoint = Read-Required "RDS endpoint (without https://)"
$RdsPort = [int](Read-Required "RDS MySQL port" "3306")
$DatabaseName = Read-Required "Database name" "civicherodb"
$DatabaseUser = Read-Required "Database username" "admin"
$DatabasePasswordSecure = Read-Host "Database password" -AsSecureString
$DatabasePassword = Convert-SecureToPlain $DatabasePasswordSecure

$AwsRegion = Read-Required "AWS Region" "ap-south-1"
$S3Bucket = Read-Required "S3 bucket name"

$AwsAccessKey = Read-Host "AWS access key ID (needed for S3 from laptop Docker)"
$AwsSecretKeySecure = Read-Host "AWS secret access key" -AsSecureString
$AwsSecretKey = Convert-SecureToPlain $AwsSecretKeySecure

$SsmTargetInstanceId = ""
$AwsProfile = "default"
$LocalTunnelPort = 13306

if ($Mode -eq "SsmTunnel") {
    $SsmTargetInstanceId = Read-Required "SSM-managed EC2 instance ID (example i-0123456789abcdef0)"
    $AwsProfile = Read-Required "AWS CLI profile" "default"
    $LocalTunnelPort = [int](Read-Required "Local tunnel port" "13306")
    $ConnectionHost = "host.docker.internal"
    $ConnectionPort = $LocalTunnelPort
}
else {
    $ConnectionHost = $RdsEndpoint
    $ConnectionPort = $RdsPort
}

New-Item -ItemType Directory -Path $SecretsRoot -Force | Out-Null

$quotedPassword = Quote-ConnectionValue $DatabasePassword
$connectionString = "Server=$ConnectionHost;Port=$ConnectionPort;Database=$DatabaseName;User ID=$DatabaseUser;Password=$quotedPassword;SslMode=Required;Connection Timeout=15;Default Command Timeout=30;"

Set-Content `
    -LiteralPath (Join-Path $SecretsRoot "rds_connection.txt") `
    -Value $connectionString `
    -Encoding UTF8 `
    -NoNewline

Ensure-File `
    -Path (Join-Path $SecretsRoot "jwt_secret.txt") `
    -Value (New-RandomSecret 64)

Set-Content `
    -LiteralPath (Join-Path $SecretsRoot "aws_access_key.txt") `
    -Value $AwsAccessKey.Trim() `
    -Encoding UTF8 `
    -NoNewline

Set-Content `
    -LiteralPath (Join-Path $SecretsRoot "aws_secret_key.txt") `
    -Value $AwsSecretKey `
    -Encoding UTF8 `
    -NoNewline

Ensure-File `
    -Path (Join-Path $SecretsRoot "ai_api_key.txt") `
    -Value ""

$rabbitPassword = New-RandomSecret 24

@"
COMPOSE_PROJECT_NAME=civichero-aws-rds
SECRETS_DIR=./.secrets
PUBLIC_HTTP_PORT=8088
BACKEND_HOST_PORT=5180
RABBITMQ_MANAGEMENT_PORT=15672
RABBITMQ_USER=civichero
RABBITMQ_PASSWORD=$rabbitPassword
AWS_REGION=$AwsRegion
AWS_BUCKET=$S3Bucket
PUBLIC_ORIGIN=http://localhost:8088
ALLOWED_HOSTS=localhost
AI_PROVIDER=RuleBased
RELEASE_VERSION=aws-rds-docker-local
COMMIT_SHA=local
"@ | Set-Content -LiteralPath $EnvFile -Encoding UTF8

$config = [ordered]@{
    Mode = $Mode
    RdsEndpoint = $RdsEndpoint
    RdsPort = $RdsPort
    DatabaseName = $DatabaseName
    DatabaseUser = $DatabaseUser
    AwsRegion = $AwsRegion
    S3Bucket = $S3Bucket
    SsmTargetInstanceId = $SsmTargetInstanceId
    AwsProfile = $AwsProfile
    LocalTunnelPort = $LocalTunnelPort
}

$config |
    ConvertTo-Json -Depth 5 |
    Set-Content -LiteralPath $ConfigFile -Encoding UTF8

$gitIgnore = Join-Path $ProjectRoot ".gitignore"
$ignoreEntries = @(
    "deployment/.env.aws-rds",
    "deployment/.aws-rds-docker.json",
    "deployment/.secrets/",
    "deployment/.aws-rds-tunnel.pid"
)

if (-not (Test-Path -LiteralPath $gitIgnore)) {
    New-Item -ItemType File -Path $gitIgnore -Force | Out-Null
}

$ignoreContent = Get-Content -LiteralPath $gitIgnore -Raw
foreach ($entry in $ignoreEntries) {
    $escapedEntry = [System.Text.RegularExpressions.Regex]::Escape($entry)
    if ($ignoreContent -notmatch "(?m)^$escapedEntry\s*$") {
        Add-Content -LiteralPath $gitIgnore -Value $entry
    }
}

Write-Host ""
Write-Host "Configuration completed." -ForegroundColor Green
Write-Host "Mode: $Mode"
Write-Host "RDS endpoint: $RdsEndpoint"
Write-Host "Database: $DatabaseName"
Write-Host "S3 bucket: $S3Bucket"
Write-Host ""
Write-Host "Connection-string password and AWS keys were written only to ignored secret files." -ForegroundColor Yellow
Write-Host "Next: run start-aws-rds.bat from the project root." -ForegroundColor Green


