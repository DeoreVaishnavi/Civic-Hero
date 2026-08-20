param(
    [string]$BaseUrl = "http://localhost:5173",
    [int]$Attempts = 18,
    [int]$DelaySeconds = 5,
    [switch]$RequireReadiness
)

$ErrorActionPreference = "Stop"
$BaseUrl = $BaseUrl.TrimEnd('/')
$checks = @(
    @{ Name = "Nginx edge"; Path = "/nginx-health" },
    @{ Name = "Backend liveness"; Path = "/health/live" },
    @{ Name = "React frontend"; Path = "/" }
)

if ($RequireReadiness) {
    $checks += @{ Name = "Backend readiness"; Path = "/health/ready" }
}

for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
    $failed = @()
    foreach ($check in $checks) {
        try {
            $response = Invoke-WebRequest -Uri ($BaseUrl + $check.Path) -UseBasicParsing -TimeoutSec 15
            if ($response.StatusCode -lt 200 -or $response.StatusCode -ge 400) {
                $failed += "$($check.Name): HTTP $($response.StatusCode)"
            }
        }
        catch { $failed += "$($check.Name): $($_.Exception.Message)" }
    }

    if ($failed.Count -eq 0) {
        Write-Host "All CivicHero deployment checks passed." -ForegroundColor Green
        exit 0
    }

    if ($attempt -eq $Attempts) {
        $failed | ForEach-Object { Write-Host $_ -ForegroundColor Red }
        throw "Deployment verification failed after $Attempts attempts."
    }

    Write-Host "Services are still starting ($attempt/$Attempts)..." -ForegroundColor Yellow
    Start-Sleep -Seconds $DelaySeconds
}
