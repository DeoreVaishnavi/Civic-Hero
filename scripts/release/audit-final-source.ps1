param([string]$ProjectRoot = "C:\Users\vaish\Music\CivicHeroSolution")
$ErrorActionPreference = "Stop"
$ProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot)
$critical = @(
 "backend\CivicHero.Backend\Program.cs",
 "backend\CivicHero.Backend\Controllers\AuthController.cs",
 "backend\CivicHero.Backend\Controllers\ComplaintsController.cs",
 "backend\CivicHero.Backend\Controllers\AssignmentsController.cs",
 "backend\CivicHero.Backend\Controllers\VerificationController.cs",
 "backend\CivicHero.Backend\Controllers\RewardsController.cs",
 "backend\CivicHero.Backend\Infrastructure\Caching\RedisCacheService.cs",
 "backend\CivicHero.Backend\Infrastructure\Messaging\RabbitMqPublisher.cs",
 "frontend\civichero-web\src\routes\AppRoutes.jsx",
 "frontend\civichero-web\src\pages\admin\FinalReleaseCenter.jsx",
 "deployment\backend\Dockerfile", "deployment\frontend\Dockerfile",
 "deployment\docker-compose.yml", "deployment\nginx\nginx.conf"
)
$issues = @()
foreach ($relative in $critical) {
    $path = Join-Path $ProjectRoot $relative
    if (-not (Test-Path $path)) { $issues += [pscustomobject]@{ path=$relative; issue="Missing" }; continue }
    if ((Get-Item $path).Length -eq 0) { $issues += [pscustomobject]@{ path=$relative; issue="Empty" } }
}
$patterns = @("TODO: implement", "throw new NotImplementedException", "mockData", "demo-only")
$matches = @()
Get-ChildItem (Join-Path $ProjectRoot "backend\CivicHero.Backend"),(Join-Path $ProjectRoot "frontend\civichero-web\src") -Recurse -File -ErrorAction SilentlyContinue | ForEach-Object {
    $file = $_
    foreach ($pattern in $patterns) {
        $found = Select-String -Path $file.FullName -Pattern $pattern -SimpleMatch -ErrorAction SilentlyContinue
        foreach ($match in $found) { $matches += [pscustomobject]@{ path=$file.FullName.Substring($ProjectRoot.Length).TrimStart('\'); pattern=$pattern; line=$match.LineNumber } }
    }
}
$reportDir = Join-Path $ProjectRoot "artifacts\phase18\source-audit"
New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
[ordered]@{ criticalIssues=$issues; reviewMatches=$matches; generatedAtUtc=(Get-Date).ToUniversalTime().ToString('o') } | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $reportDir "final-source-audit.json") -Encoding UTF8
if ($issues.Count -gt 0) { throw "Critical source audit found missing or empty files. Review artifacts/phase18/source-audit/final-source-audit.json" }
Write-Host "Critical source audit passed. Review non-blocking pattern matches manually." -ForegroundColor Green
