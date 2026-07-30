param(
    [Parameter(Mandatory = $true)]
    [string]$DbInstanceIdentifier,
    [string]$AwsRegion = "ap-south-1",
    [string]$SnapshotIdentifier
)

$ErrorActionPreference = "Stop"
if (-not (Get-Command aws -ErrorAction SilentlyContinue)) { throw "AWS CLI is not installed." }
if ([string]::IsNullOrWhiteSpace($SnapshotIdentifier)) {
    $SnapshotIdentifier = "civichero-predeploy-$(Get-Date -Format 'yyyyMMdd-HHmmss')".ToLowerInvariant()
}

Write-Host "Creating AWS RDS snapshot $SnapshotIdentifier..." -ForegroundColor Cyan
aws rds create-db-snapshot `
    --region $AwsRegion `
    --db-instance-identifier $DbInstanceIdentifier `
    --db-snapshot-identifier $SnapshotIdentifier | Out-Null
if ($LASTEXITCODE -ne 0) { throw "RDS snapshot request failed." }

aws rds wait db-snapshot-available `
    --region $AwsRegion `
    --db-snapshot-identifier $SnapshotIdentifier
if ($LASTEXITCODE -ne 0) { throw "RDS snapshot did not become available." }

Write-Host "RDS snapshot is available: $SnapshotIdentifier" -ForegroundColor Green
