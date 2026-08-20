param(
    [string]$ProjectRoot = "C:\Users\vaish\Music\CivicHeroSolution",
    [int]$Port = 33080
)
$ErrorActionPreference = "Stop"
$ProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot)
$backend = Join-Path $ProjectRoot "backend\CivicHero.Backend"
$artifacts = Join-Path $ProjectRoot "artifacts\phase18\migrations"
New-Item -ItemType Directory -Path $artifacts -Force | Out-Null
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) { throw "Docker is required for clean migration validation." }
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw ".NET 8 SDK is required." }
$name = "civichero-phase18-mysql-$PID"
$password = "CivicHero-Phase18-Only-Password"
$connection = "Server=127.0.0.1;Port=$Port;Database=civichero_clean;Uid=root;Pwd=$password;SslMode=None;AllowPublicKeyRetrieval=True;"
try {
    docker run -d --name $name -e "MYSQL_ROOT_PASSWORD=$password" -e "MYSQL_DATABASE=civichero_clean" -p "${Port}:3306" mysql:8.0 --default-authentication-plugin=mysql_native_password | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Unable to start MySQL 8 validation container." }
    $ready = $false
    for ($i = 0; $i -lt 60; $i++) {
        docker exec $name mysqladmin ping -uroot "-p$password" --silent *> $null
        if ($LASTEXITCODE -eq 0) { $ready = $true; break }
        Start-Sleep -Seconds 2
    }
    if (-not $ready) { throw "MySQL validation container did not become ready." }
    Push-Location $backend
    try {
        dotnet ef database update --context CivicDbContext --connection $connection
        if ($LASTEXITCODE -ne 0) { throw "EF Core failed to apply migrations to a clean MySQL 8 database." }
        $migrations = dotnet ef migrations list --context CivicDbContext
        if ($LASTEXITCODE -ne 0) { throw "Unable to list EF Core migrations." }
    } finally { Pop-Location }
    $tables = docker exec $name mysql -uroot "-p$password" -N -e "USE civichero_clean; SHOW TABLES;"
    if ($LASTEXITCODE -ne 0 -or @($tables).Count -lt 5) { throw "Clean database contains too few tables after migration." }
    [ordered]@{ passed = $true; database = "civichero_clean"; tableCount = @($tables).Count; migrations = @($migrations); validatedAtUtc = (Get-Date).ToUniversalTime().ToString('o') } |
        ConvertTo-Json -Depth 5 | Set-Content (Join-Path $artifacts "clean-migration.json") -Encoding UTF8
} finally {
    docker rm -f $name *> $null
}
Write-Host "Clean MySQL 8 migration validation passed." -ForegroundColor Green
