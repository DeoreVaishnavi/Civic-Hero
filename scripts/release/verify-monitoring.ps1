param(
    [string]$ProjectRoot = "C:\Users\vaish\Music\CivicHeroSolution",
    [string]$ApplicationUrl = "http://localhost:5173",
    [string]$PrometheusUrl = "http://localhost:9090",
    [string]$GrafanaUrl = "http://localhost:3000"
)
$ErrorActionPreference = "Stop"
$checks = @(
    @{ Name = "Application live"; Url = "$($ApplicationUrl.TrimEnd('/'))/health/live" },
    @{ Name = "Application ready"; Url = "$($ApplicationUrl.TrimEnd('/'))/health/ready" },
    @{ Name = "Prometheus ready"; Url = "$($PrometheusUrl.TrimEnd('/'))/-/ready" },
    @{ Name = "Grafana health"; Url = "$($GrafanaUrl.TrimEnd('/'))/api/health" }
)
$results = foreach ($check in $checks) {
    try { $response = Invoke-WebRequest -Uri $check.Url -UseBasicParsing -TimeoutSec 15; [pscustomobject]@{ name=$check.Name; url=$check.Url; passed=($response.StatusCode -ge 200 -and $response.StatusCode -lt 400); statusCode=$response.StatusCode } }
    catch { [pscustomobject]@{ name=$check.Name; url=$check.Url; passed=$false; statusCode=0; error=$_.Exception.Message } }
}
$path = Join-Path ([System.IO.Path]::GetFullPath($ProjectRoot)) "artifacts\phase18\monitoring"
New-Item -ItemType Directory -Path $path -Force | Out-Null
$results | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $path "monitoring-verification.json") -Encoding UTF8
if (@($results | Where-Object { -not $_.passed }).Count -gt 0) { throw "One or more monitoring checks failed." }
Write-Host "Monitoring verification passed." -ForegroundColor Green
