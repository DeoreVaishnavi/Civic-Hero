# CivicHero Phase 18 — Final Testing and Production Release

Phase 18 is an evidence-driven release phase. Installing files does not complete it.
The release is approved only when every required gate in
`artifacts/phase18/release-evidence.json` has status `Passed`.

## Required critical journey

Register → verify email → login → create complaint with image → AI triage →
assign → accept → progress → resolve with evidence → geo-verify → citizen approve
→ reward → close.

## Required release evidence

- Backend Release build and xUnit coverage
- Frontend Vite build and Vitest coverage
- Testcontainers MySQL smoke test
- Clean MySQL 8 migration from an empty database
- Playwright critical journey
- Newman API regression
- k6 thresholds
- OWASP ZAP review
- Docker image and production-like deployment verification
- Prometheus/Grafana health verification
- RDS/S3 backup restore rehearsal
- Signed UAT with at least 95% pass rate and no open Critical/Major defects
- Clean Git commit and immutable release tag

## Main commands

```powershell
powershell -ExecutionPolicy Bypass -File scripts/release/run-phase18-release-gates.ps1 `
  -ProjectRoot "C:\Users\vaish\Music\CivicHeroSolution" `
  -ReleaseVersion "v1.0.0-rc.1"

powershell -ExecutionPolicy Bypass -File scripts/release/create-release-candidate.ps1 `
  -ProjectRoot "C:\Users\vaish\Music\CivicHeroSolution" `
  -ReleaseVersion "v1.0.0"
```

Never run the destructive or data-creating E2E journey against production.
