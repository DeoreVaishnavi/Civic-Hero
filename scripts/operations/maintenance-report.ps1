param(
    [string]$ProjectRoot = "C:\Users\vaish\Music\CivicHeroSolution",
    [string]$BaseUrl = "http://localhost:8088"
)
$ErrorActionPreference = "Continue"
$ProjectRoot = [IO.Path]::GetFullPath($ProjectRoot)
$Output = Join-Path $ProjectRoot "artifacts\phase16\maintenance"
New-Item -ItemType Directory -Path $Output -Force | Out-Null

$report = [ordered]@{ generatedAt=(Get-Date).ToString('o'); baseUrl=$BaseUrl; health=@{}; disk=@{}; containers=@(); notes=@() }
foreach ($path in @("/health/live", "/health/ready")) {
    try { $report.health[$path] = Invoke-RestMethod -Uri ($BaseUrl.TrimEnd('/') + $path) -TimeoutSec 15 }
    catch { $report.health[$path] = @{ error=$_.Exception.Message } }
}
$drive = Get-PSDrive -Name ([IO.Path]::GetPathRoot($ProjectRoot).TrimEnd(':','\\')) -ErrorAction SilentlyContinue
if ($drive) { $report.disk = @{ freeBytes=$drive.Free; usedBytes=$drive.Used } }
if (Get-Command docker -ErrorAction SilentlyContinue) { $report.containers = @(docker ps --format "{{.Names}}|{{.Status}}|{{.Image}}" 2>&1) }
$report.notes += "Review AWS RDS metrics, S3 storage, failed logins, background jobs and unresolved Critical complaints in their managed consoles."
$report | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $Output "maintenance-report.json") -Encoding UTF8
Write-Host "Maintenance report generated: $Output" -ForegroundColor Green
