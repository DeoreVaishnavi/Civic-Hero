param(
    [string]$ProjectRoot = "C:\Users\vaish\Music\CivicHeroSolution",
    [string]$EvidenceFile
)
$ErrorActionPreference = "Stop"
$ProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot)
if ([string]::IsNullOrWhiteSpace($EvidenceFile)) { $EvidenceFile = Join-Path $ProjectRoot "artifacts\phase18\backup-restore-evidence.json" }
if (-not (Test-Path $EvidenceFile)) { throw "Backup restore evidence is missing: $EvidenceFile" }
$data = Get-Content $EvidenceFile -Raw | ConvertFrom-Json
if ($data.restoreSucceeded -ne $true) { throw "The backup restore rehearsal is not marked successful." }
if ([string]::IsNullOrWhiteSpace($data.snapshotIdentifier)) { throw "RDS snapshot identifier is required." }
if ([string]::IsNullOrWhiteSpace($data.restoredDatabase)) { throw "Restored non-production database identifier is required." }
if ([string]::IsNullOrWhiteSpace($data.validatedBy)) { throw "Validator name is required." }
if (@($data.validationChecks).Count -lt 3) { throw "At least three restore validation checks are required." }
Write-Host "Backup restore evidence validated." -ForegroundColor Green
