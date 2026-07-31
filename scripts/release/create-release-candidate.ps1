param(
    [string]$ProjectRoot = "C:\Users\vaish\Music\CivicHeroSolution",
    [string]$ReleaseVersion = "v1.0.0",
    [switch]$PushTag
)
$ErrorActionPreference = "Stop"
$ProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot)
if ($ReleaseVersion -notmatch '^v\d+\.\d+\.\d+([-.][0-9A-Za-z.-]+)?$') { throw "ReleaseVersion must look like v1.0.0 or v1.0.0-rc.1." }
$evidencePath = Join-Path $ProjectRoot "artifacts\phase18\release-evidence.json"
if (-not (Test-Path $evidencePath)) { throw "Run Phase 18 release gates first." }
$evidence = Get-Content $evidencePath -Raw | ConvertFrom-Json
if ($evidence.approved -ne $true) { throw "Release evidence is not approved." }
if ($evidence.releaseVersion -ne $ReleaseVersion -and $evidence.releaseVersion -ne "$ReleaseVersion-rc.1") { Write-Warning "Evidence release version differs from requested final version." }
if (-not (Get-Command git -ErrorAction SilentlyContinue)) { throw "Git is required." }
Push-Location $ProjectRoot
try {
    if (git status --porcelain) { throw "Working tree is not clean." }
    $commit = (git rev-parse HEAD).Trim()
    if ($commit -ne $evidence.gitCommit) { throw "Current Git commit differs from the tested release evidence commit." }
    git rev-parse $ReleaseVersion *> $null
    if ($LASTEXITCODE -eq 0) { throw "Git tag already exists: $ReleaseVersion" }
    git tag -a $ReleaseVersion -m "CivicHero production release $ReleaseVersion"
    if ($LASTEXITCODE -ne 0) { throw "Unable to create release tag." }
    $output = Join-Path $ProjectRoot "artifacts\phase18\release-candidate"
    New-Item -ItemType Directory -Path $output -Force | Out-Null
    $archive = Join-Path $output "CivicHero-$($ReleaseVersion.TrimStart('v')).zip"
    git archive --format=zip --output=$archive $ReleaseVersion
    if ($LASTEXITCODE -ne 0) { throw "Unable to create Git release archive." }
    $hash = (Get-FileHash $archive -Algorithm SHA256).Hash
    [ordered]@{ releaseVersion=$ReleaseVersion; gitCommit=$commit; archive=(Split-Path $archive -Leaf); sha256=$hash; createdAtUtc=(Get-Date).ToUniversalTime().ToString('o'); evidence="../release-evidence.json" } | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $output "release-manifest.json") -Encoding UTF8
    if ($PushTag) { git push origin $ReleaseVersion; if ($LASTEXITCODE -ne 0) { throw "Unable to push release tag." } }
} finally { Pop-Location }
Write-Host "Release candidate created and tagged: $ReleaseVersion" -ForegroundColor Green
