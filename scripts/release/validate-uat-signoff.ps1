param(
    [string]$ProjectRoot = "C:\Users\vaish\Music\CivicHeroSolution",
    [string]$EvidenceFile
)
$ErrorActionPreference = "Stop"
$ProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot)
if ([string]::IsNullOrWhiteSpace($EvidenceFile)) { $EvidenceFile = Join-Path $ProjectRoot "tests\uat\phase18-uat-signoff.json" }
if (-not (Test-Path $EvidenceFile)) { throw "UAT evidence is missing: $EvidenceFile. Copy the example, enter real results and signatures, then rerun." }
$data = Get-Content $EvidenceFile -Raw | ConvertFrom-Json
if ($data.approved -ne $true) { throw "UAT is not approved." }
if ([double]$data.passRate -lt 95) { throw "UAT pass rate must be at least 95%." }
if ([int]$data.openCriticalDefects -ne 0 -or [int]$data.openMajorDefects -ne 0) { throw "Critical and Major defects must be zero." }
if (@($data.participants).Count -lt 2) { throw "At least two real UAT participants are required." }
if (@($data.signatures).Count -lt 2) { throw "At least two approval signatures are required." }
Write-Host "UAT evidence validated: $EvidenceFile" -ForegroundColor Green
