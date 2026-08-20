param(
    [string]$ProjectRoot = "C:\Users\vaish\Music\CivicHeroSolution",
    [string]$BaseUrl = "http://localhost:5173"
)
$ErrorActionPreference = "Stop"
$ProjectRoot = [IO.Path]::GetFullPath($ProjectRoot)
$Output = Join-Path $ProjectRoot "artifacts\phase16\go-live"
New-Item -ItemType Directory -Path $Output -Force | Out-Null

$checks = @()
function Add-Check($Name, $Passed, $Detail) { $script:checks += [pscustomobject]@{ Name=$Name; Passed=[bool]$Passed; Detail=$Detail } }

foreach ($path in @("/nginx-health", "/health/live", "/health/ready")) {
    try {
        $response = Invoke-WebRequest -Uri ($BaseUrl.TrimEnd('/') + $path) -UseBasicParsing -TimeoutSec 20
        Add-Check $path ($response.StatusCode -eq 200) "HTTP $($response.StatusCode)"
    } catch { Add-Check $path $false $_.Exception.Message }
}

$required = @(
    "docs\requirements\SRS.md",
    "docs\architecture\HLD.md",
    "docs\architecture\LLD.md",
    "docs\FINAL-PROJECT-REPORT.md",
    "docs\handover\PROJECT-HANDOVER.md",
    "tests\uat\UAT-SIGNOFF-TEMPLATE.md"
)
foreach ($relative in $required) { Add-Check $relative (Test-Path (Join-Path $ProjectRoot $relative)) "Required launch artifact" }

$checks | Export-Csv (Join-Path $Output "go-live-validation.csv") -NoTypeInformation
$summary = [pscustomobject]@{ GeneratedAt=(Get-Date).ToString('o'); BaseUrl=$BaseUrl; Passed=($checks | Where-Object Passed).Count; Total=$checks.Count; AllPassed=($checks | Where-Object { -not $_.Passed }).Count -eq 0; Note="Technical checks only. Stakeholder approval and UAT signatures remain manual evidence." }
$summary | ConvertTo-Json | Set-Content (Join-Path $Output "go-live-summary.json") -Encoding UTF8
$checks | Format-Table -AutoSize
if (-not $summary.AllPassed) { throw "One or more go-live technical checks failed. Review $Output" }
Write-Host "Technical go-live validation passed. Manual approvals are still required." -ForegroundColor Green
