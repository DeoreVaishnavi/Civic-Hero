param(
    [string]$ProjectRoot = "C:\CivicHeroSolution",
    [string]$RdsEndpoint,
    [string]$DatabaseName = "civicherodb",
    [string]$DatabaseUser = "admin",
    [string]$DatabasePassword,
    [string]$AwsRegion = "ap-south-1",
    [string]$BucketName = "civichero-storage",
    [string]$AwsAccessKey,
    [string]$AwsSecretKey
)

$ErrorActionPreference = "Stop"
$backendDirectory = Join-Path ([System.IO.Path]::GetFullPath($ProjectRoot)) "backend\CivicHero.Backend"
$projectFile = Join-Path $backendDirectory "CivicHero.Backend.csproj"

if (-not (Test-Path $projectFile)) {
    throw "Missing backend project: $projectFile"
}

function Read-PlainSecret([string]$Prompt) {
    $secure = Read-Host $Prompt -AsSecureString
    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    }
}

if ([string]::IsNullOrWhiteSpace($RdsEndpoint)) {
    $RdsEndpoint = Read-Host "Enter AWS RDS MySQL endpoint (without port)"
}
if ([string]::IsNullOrWhiteSpace($DatabasePassword)) {
    $DatabasePassword = Read-PlainSecret "Enter RDS database password"
}

$escapedPassword = $DatabasePassword.Replace("'", "''")
$connectionString = "Server=$RdsEndpoint;Port=3306;Database=$DatabaseName;Uid=$DatabaseUser;Pwd='$escapedPassword';SslMode=Required;"

Push-Location $backendDirectory
try {
    dotnet user-secrets set "ConnectionStrings:DefaultConnection" $connectionString
    dotnet user-secrets set "AWS:Region" $AwsRegion
    dotnet user-secrets set "AWS:BucketName" $BucketName

    if (-not [string]::IsNullOrWhiteSpace($AwsAccessKey)) {
        dotnet user-secrets set "AWS:AccessKey" $AwsAccessKey
    }

    if ([string]::IsNullOrWhiteSpace($AwsAccessKey)) {
        $configureKeys = Read-Host "Store AWS access keys in user-secrets? Type Y only if AWS CLI/profile is unavailable"
        if ($configureKeys -match '^[Yy]$') {
            $AwsAccessKey = Read-Host "Enter AWS access key"
            $AwsSecretKey = Read-PlainSecret "Enter AWS secret key"
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($AwsAccessKey)) {
        if ([string]::IsNullOrWhiteSpace($AwsSecretKey)) {
            $AwsSecretKey = Read-PlainSecret "Enter AWS secret key"
        }
        dotnet user-secrets set "AWS:AccessKey" $AwsAccessKey
        dotnet user-secrets set "AWS:SecretKey" $AwsSecretKey
    }

    Write-Host ""
    Write-Host "Phase 2 secrets configured for local development." -ForegroundColor Green
    Write-Host "Actual values were not written to appsettings.json." -ForegroundColor Green
    Write-Host ""
    Write-Host "Apply the database migration:" -ForegroundColor Yellow
    Write-Host "  dotnet ef database update --context CivicDbContext"
}
finally {
    Pop-Location
}
