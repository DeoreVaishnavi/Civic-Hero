# Phase 14 — Testing, Quality Assurance & UAT

## Purpose
Phase 14 converts CivicHero's implemented features into a repeatable release-quality process. It adds automated backend, frontend, API, browser and performance tests, plus security and UAT templates.

## Test pyramid
- Unit tests: approximately 60% of the suite.
- Integration tests: approximately 20%.
- API tests: approximately 15%.
- Browser E2E tests: approximately 5%.

## Coverage targets
- Domain logic: 90%.
- Application services: 85%.
- API: 80%.
- Frontend: 75%.
- Critical complaint lifecycle flows: 100% scenario coverage.

The first installed baseline uses lower automated frontend thresholds so the new suite can be introduced without pretending the historical code already meets the final target. Increase thresholds as coverage grows.

## Commands
```powershell
powershell -ExecutionPolicy Bypass -File scripts/run-quality-gates.ps1 -ProjectRoot "C:\Users\vaish\Music\CivicHeroSolution"
```

Backend only:
```powershell
dotnet test backend/CivicHero.Backend.Tests/CivicHero.Backend.Tests.csproj --collect:"XPlat Code Coverage"
```

Frontend only:
```powershell
cd frontend/civichero-web
npm run test:coverage
npm run test:e2e
```

k6 through Docker:
```powershell
Get-Content tests/performance/health-smoke.js | docker run --rm -i grafana/k6 run -
```

## Release rule
A release is not considered approved merely because suites are configured. Actual reports must pass, optional skipped tools must be documented, UAT must reach at least 95%, and no Critical or Major defect may remain open.
