param(
    [string]$ProjectRoot = "C:\CivicHeroSolution",
    [switch]$SkipBuild,
    [switch]$SkipMigration,
    [switch]$ApplyDatabase
)

$ErrorActionPreference = "Stop"
$ProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot)
$SourceRoot = Join-Path $PSScriptRoot "phase3-files"
$BackendDirectory = Join-Path $ProjectRoot "backend\CivicHero.Backend"
$BackendProject = Join-Path $BackendDirectory "CivicHero.Backend.csproj"
$FrontendDirectory = Join-Path $ProjectRoot "frontend\civichero-web"

if (-not (Test-Path $SourceRoot)) { throw "Phase 3 files not found: $SourceRoot" }
if (-not (Test-Path $BackendProject)) { throw "Missing backend project: $BackendProject" }
if (-not (Test-Path (Join-Path $FrontendDirectory "package.json"))) { throw "Missing frontend project." }

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupRoot = Join-Path $ProjectRoot ".phase-backups\phase3-$timestamp"

Write-Host "Applying CivicHero Phase 3 - Authentication" -ForegroundColor Cyan
Write-Host "Project: $ProjectRoot"
Write-Host "Backup:  $backupRoot"

Get-ChildItem -Path $SourceRoot -Recurse -File | ForEach-Object {
    $relativePath = $_.FullName.Substring($SourceRoot.Length).TrimStart('\', '/')
    $destination = Join-Path $ProjectRoot $relativePath
    New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null

    if (Test-Path $destination) {
        $backupDestination = Join-Path $backupRoot $relativePath
        New-Item -ItemType Directory -Path (Split-Path $backupDestination -Parent) -Force | Out-Null
        Copy-Item $destination $backupDestination -Force
    }

    Copy-Item $_.FullName $destination -Force
    Write-Host "UPDATED $relativePath" -ForegroundColor Green
}

if (-not $SkipBuild) {
    Push-Location $BackendDirectory
    try {
        Write-Host "Restoring backend packages..." -ForegroundColor Cyan
        dotnet restore
        if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed." }

        Write-Host "Building backend..." -ForegroundColor Cyan
        dotnet build --no-restore
        if ($LASTEXITCODE -ne 0) { throw "dotnet build failed." }
    }
    finally { Pop-Location }
}

if (-not $SkipMigration) {
    Push-Location $BackendDirectory
    try {
        dotnet ef --version *> $null
        if ($LASTEXITCODE -ne 0) {
            dotnet tool install --global dotnet-ef --version 8.0.29
            if ($LASTEXITCODE -ne 0) { throw "Unable to install dotnet-ef." }
        }

        $existing = Get-ChildItem (Join-Path $BackendDirectory "Migrations") -Filter "*.cs" -File -ErrorAction SilentlyContinue |
            Select-String -Pattern "Phase3AuthenticationFoundation" -Quiet

        if (-not $existing) {
            dotnet ef migrations add Phase3AuthenticationFoundation --context CivicDbContext --output-dir Migrations
            if ($LASTEXITCODE -ne 0) { throw "Phase 3 migration creation failed." }
            Write-Host "Created migration: Phase3AuthenticationFoundation" -ForegroundColor Green
        }
        else {
            Write-Host "Phase 3 migration already exists; skipped." -ForegroundColor Yellow
        }

        if ($ApplyDatabase) {
            dotnet ef database update --context CivicDbContext
            if ($LASTEXITCODE -ne 0) { throw "AWS RDS migration failed." }
            Write-Host "AWS RDS database updated." -ForegroundColor Green
        }
    }
    finally { Pop-Location }
}

if (-not $SkipBuild) {
    Push-Location $FrontendDirectory
    try {
        Write-Host "Installing frontend packages..." -ForegroundColor Cyan
        npm install
        if ($LASTEXITCODE -ne 0) { throw "npm install failed." }

        Write-Host "Building frontend..." -ForegroundColor Cyan
        npm run build
        if ($LASTEXITCODE -ne 0) { throw "npm run build failed." }
    }
    finally { Pop-Location }
}

Write-Host ""
Write-Host "Phase 3 files applied successfully." -ForegroundColor Green
Write-Host "Next run configure-phase3-secrets.ps1, then apply the database migration." -ForegroundColor Yellow
