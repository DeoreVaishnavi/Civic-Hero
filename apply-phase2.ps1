param(
    [string]$ProjectRoot = "C:\CivicHeroSolution",
    [switch]$SkipBuild,
    [switch]$SkipMigration,
    [switch]$ApplyDatabase
)

$ErrorActionPreference = "Stop"
$ProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot)
$SourceRoot = Join-Path $PSScriptRoot "phase2-files"

if (-not (Test-Path $SourceRoot)) {
    throw "Phase 2 files were not found at: $SourceRoot"
}

$backendDirectory = Join-Path $ProjectRoot "backend\CivicHero.Backend"
$backendProject = Join-Path $backendDirectory "CivicHero.Backend.csproj"
$frontendDirectory = Join-Path $ProjectRoot "frontend\civichero-web"
$frontendPackage = Join-Path $frontendDirectory "package.json"

if (-not (Test-Path $backendProject)) {
    throw "Invalid project folder. Missing: $backendProject"
}
if (-not (Test-Path $frontendPackage)) {
    throw "Invalid project folder. Missing: $frontendPackage"
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupRoot = Join-Path $ProjectRoot ".phase-backups\phase2-$timestamp"

Write-Host ""
Write-Host "Applying CivicHero Phase 2 - AWS RDS MySQL + Amazon S3" -ForegroundColor Cyan
Write-Host "Project: $ProjectRoot"
Write-Host "Backup:  $backupRoot"
Write-Host ""

$sourceFiles = Get-ChildItem -Path $SourceRoot -Recurse -File
foreach ($sourceFile in $sourceFiles) {
    $relativePath = $sourceFile.FullName.Substring($SourceRoot.Length).TrimStart('\', '/')
    $destination = Join-Path $ProjectRoot $relativePath
    $destinationDirectory = Split-Path $destination -Parent
    New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null

    if (Test-Path $destination) {
        $backupDestination = Join-Path $backupRoot $relativePath
        $backupDirectory = Split-Path $backupDestination -Parent
        New-Item -ItemType Directory -Path $backupDirectory -Force | Out-Null
        Copy-Item $destination $backupDestination -Force
    }

    Copy-Item $sourceFile.FullName $destination -Force
    Write-Host "UPDATED $relativePath" -ForegroundColor Green
}

Write-Host ""
Write-Host "Phase 2 source files applied." -ForegroundColor Green

if (-not $SkipBuild) {
    Write-Host ""
    Write-Host "Restoring and building backend..." -ForegroundColor Cyan
    Push-Location $backendDirectory
    try {
        dotnet restore
        if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed." }

        dotnet build --no-restore
        if ($LASTEXITCODE -ne 0) { throw "dotnet build failed." }
    }
    finally {
        Pop-Location
    }
}

if (-not $SkipMigration) {
    Write-Host ""
    Write-Host "Preparing EF Core migration..." -ForegroundColor Cyan

    $migrationDirectory = Join-Path $backendDirectory "Migrations"
    New-Item -ItemType Directory -Path $migrationDirectory -Force | Out-Null

    $emptyMigrationFiles = Get-ChildItem $migrationDirectory -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Length -eq 0 }

    foreach ($emptyFile in $emptyMigrationFiles) {
        $relative = "backend\CivicHero.Backend\Migrations\$($emptyFile.Name)"
        $backupDestination = Join-Path $backupRoot $relative
        New-Item -ItemType Directory -Path (Split-Path $backupDestination -Parent) -Force | Out-Null
        Copy-Item $emptyFile.FullName $backupDestination -Force
        Remove-Item $emptyFile.FullName -Force
        Write-Host "REMOVED EMPTY PLACEHOLDER $relative" -ForegroundColor Yellow
    }

    Push-Location $backendDirectory
    try {
        dotnet ef --version *> $null
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Installing dotnet-ef 8.0.29..." -ForegroundColor Yellow
            dotnet tool install --global dotnet-ef --version 8.0.29
            if ($LASTEXITCODE -ne 0) { throw "Unable to install dotnet-ef." }
        }

        $hasMigration = Get-ChildItem $migrationDirectory -Filter "*.cs" -File -ErrorAction SilentlyContinue |
            Select-String -Pattern ": Migration" -Quiet

        if (-not $hasMigration) {
            dotnet ef migrations add Phase2DatabaseFoundation `
                --context CivicDbContext `
                --output-dir Migrations
            if ($LASTEXITCODE -ne 0) { throw "EF Core migration creation failed." }
            Write-Host "Created migration: Phase2DatabaseFoundation" -ForegroundColor Green
        }
        else {
            Write-Host "A real EF Core migration already exists; migration creation skipped." -ForegroundColor Yellow
        }

        if ($ApplyDatabase) {
            Write-Host "Applying migration to AWS RDS MySQL..." -ForegroundColor Cyan
            dotnet ef database update --context CivicDbContext
            if ($LASTEXITCODE -ne 0) {
                throw "Database update failed. Verify RDS endpoint, credentials, TLS and security-group access."
            }
            Write-Host "AWS RDS database updated successfully." -ForegroundColor Green
        }
    }
    finally {
        Pop-Location
    }
}

if (-not $SkipBuild) {
    Write-Host ""
    Write-Host "Building frontend..." -ForegroundColor Cyan
    Push-Location $frontendDirectory
    try {
        npm install
        if ($LASTEXITCODE -ne 0) { throw "npm install failed." }

        npm run build
        if ($LASTEXITCODE -ne 0) { throw "npm run build failed." }
    }
    finally {
        Pop-Location
    }
}

Write-Host ""
Write-Host "Phase 2 code installation complete." -ForegroundColor Green
Write-Host ""
Write-Host "Next configure local secrets:" -ForegroundColor Yellow
Write-Host "  powershell -ExecutionPolicy Bypass -File `"$PSScriptRoot\configure-phase2-secrets.ps1`" -ProjectRoot `"$ProjectRoot`""
Write-Host ""
Write-Host "After secrets are configured, apply the RDS migration:" -ForegroundColor Yellow
Write-Host "  cd $backendDirectory"
Write-Host "  dotnet ef database update --context CivicDbContext"
Write-Host ""
Write-Host "Start backend:" -ForegroundColor Yellow
Write-Host "  cd $backendDirectory"
Write-Host "  dotnet run --launch-profile http"
Write-Host ""
Write-Host "Start frontend in a second PowerShell window:" -ForegroundColor Yellow
Write-Host "  cd $frontendDirectory"
Write-Host "  npm run dev"
