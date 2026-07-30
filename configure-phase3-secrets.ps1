param([string]$ProjectRoot = "C:\CivicHeroSolution")

$ErrorActionPreference = "Stop"
$BackendDirectory = Join-Path ([System.IO.Path]::GetFullPath($ProjectRoot)) "backend\CivicHero.Backend"
if (-not (Test-Path (Join-Path $BackendDirectory "CivicHero.Backend.csproj"))) {
    throw "CivicHero backend project was not found."
}

Write-Host "Configuring CivicHero Phase 3 JWT secret" -ForegroundColor Cyan
$answer = Read-Host "Press Enter to generate a secure JWT key, or type your own key (minimum 32 characters)"

if ([string]::IsNullOrWhiteSpace($answer)) {
    $bytes = New-Object byte[] 64
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
    $secret = [Convert]::ToBase64String($bytes)
}
else {
    $secret = $answer
}

if ([Text.Encoding]::UTF8.GetByteCount($secret) -lt 32) {
    throw "JWT key must contain at least 32 bytes."
}

Push-Location $BackendDirectory
try {
    dotnet user-secrets set "Jwt:SecretKey" $secret
    if ($LASTEXITCODE -ne 0) { throw "Unable to save the JWT secret." }
}
finally { Pop-Location }

Write-Host "JWT secret stored in .NET user-secrets. It was not written to source control." -ForegroundColor Green
