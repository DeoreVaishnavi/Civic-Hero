
param([string]$ProjectRoot = "C:\Users\vaish\Music\CivicHeroSolution")
$ErrorActionPreference = "Stop"
$ProjectRoot = [IO.Path]::GetFullPath($ProjectRoot)
$ReportDirectory = Join-Path $ProjectRoot "artifacts\phase17"
New-Item -ItemType Directory -Path $ReportDirectory -Force | Out-Null
$extensions = @("*.cs", "*.js", "*.jsx", "*.json", "*.ps1", "*.yml", "*.yaml", "*.md")
$files = foreach ($pattern in $extensions) { Get-ChildItem $ProjectRoot -Recurse -File -Filter $pattern -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(node_modules|bin|obj|dist|coverage|TestResults|\.git|\.phase-backups)\\' } }
$empty = $files | Where-Object { $_.Length -eq 0 } | Select-Object -ExpandProperty FullName
$mock = $files | Select-String -Pattern '(?i)mockData|demoOnly|hard-coded token|fake API|TODO: implement' -List | Select-Object Path, LineNumber, Line
$critical = @(
  'backend\CivicHero.Backend\Infrastructure\Caching\RedisCacheService.cs',
  'backend\CivicHero.Backend\Infrastructure\Messaging\RabbitMqPublisher.cs',
  'backend\CivicHero.Backend\Infrastructure\Messaging\RabbitMqConsumer.cs',
  'backend\CivicHero.Backend\Infrastructure\BackgroundServices\SlaMonitorBackgroundService.cs',
  'backend\CivicHero.Backend\Infrastructure\Security\TwoFactorService.cs'
)
$criticalFailures = foreach ($relative in $critical) { $path = Join-Path $ProjectRoot $relative; if (-not (Test-Path $path) -or (Get-Item $path).Length -eq 0) { $relative } }
$report = [ordered]@{ generatedAt = (Get-Date).ToString('o'); projectRoot = $ProjectRoot; scannedFiles = @($files).Count; emptyFileCount = @($empty).Count; emptyFiles = @($empty | ForEach-Object { $_.Substring($ProjectRoot.Length).TrimStart('\') }); mockReferenceCount = @($mock).Count; mockReferences = @($mock); criticalFailures = @($criticalFailures) }
$reportPath = Join-Path $ReportDirectory 'gap-audit.json'
$report | ConvertTo-Json -Depth 6 | Set-Content $reportPath -Encoding UTF8
Write-Host "Phase 17 gap audit: $reportPath" -ForegroundColor Cyan
Write-Host "Empty source/config files: $(@($empty).Count)" -ForegroundColor $(if (@($empty).Count) { 'Yellow' } else { 'Green' })
Write-Host "Mock/TODO references: $(@($mock).Count)" -ForegroundColor $(if (@($mock).Count) { 'Yellow' } else { 'Green' })
if (@($criticalFailures).Count) { throw "Critical Phase 17 files are missing or empty: $($criticalFailures -join ', ')" }
