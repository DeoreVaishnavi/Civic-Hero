
param([string]$ProjectRoot = "C:\Users\vaish\Music\CivicHeroSolution", [string]$BaseUrl = "http://localhost:5180", [string]$AccessToken = $env:CIVICHERO_ADMIN_TOKEN)
$ErrorActionPreference = "Stop"
$headers = @{ 'X-Correlation-ID' = [guid]::NewGuid().ToString('N') }
if (-not [string]::IsNullOrWhiteSpace($AccessToken)) { $headers.Authorization = "Bearer $AccessToken" }
$results = @()
foreach ($path in @('/health/live','/health/ready')) { try { $response = Invoke-WebRequest "$BaseUrl$path" -Headers $headers -UseBasicParsing; $results += [pscustomobject]@{ endpoint=$path; status=$response.StatusCode; passed=$true } } catch { $results += [pscustomobject]@{ endpoint=$path; status=$_.Exception.Response.StatusCode.value__; passed=$false } } }
if (-not [string]::IsNullOrWhiteSpace($AccessToken)) {
  foreach ($item in @(@{path='/api/v1/integration/readiness';method='GET'},@{path='/api/v1/integration/cache/test';method='POST'},@{path='/api/v1/integration/messaging/test';method='POST'})) { try { $response = Invoke-WebRequest "$BaseUrl$($item.path)" -Method $item.method -Headers $headers -ContentType 'application/json' -UseBasicParsing; $results += [pscustomobject]@{ endpoint=$item.path; status=$response.StatusCode; passed=$true } } catch { $results += [pscustomobject]@{ endpoint=$item.path; status=$_.Exception.Response.StatusCode.value__; passed=$false } } }
} else { Write-Host 'CIVICHERO_ADMIN_TOKEN not set; authenticated integration endpoints were skipped.' -ForegroundColor Yellow }
$out = Join-Path $ProjectRoot 'artifacts\phase17\integration-test-results.json'; New-Item -ItemType Directory -Path (Split-Path $out) -Force | Out-Null; $results | ConvertTo-Json | Set-Content $out -Encoding UTF8; $results | Format-Table -AutoSize
if ($results.Where({ -not $_.passed }).Count) { throw "One or more Phase 17 integration checks failed. Review $out" }
