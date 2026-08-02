# CivicHero Final UI Contrast Audit

CivicHero uses one light-first civic design system:

- Light canvas `#F7F8FA` with dark text `#1B2430`.
- Dark sidebar, code and intentional dark panels use `#1E2430` with `#F5F6F8`.
- Pastel status surfaces use dark matching text.
- Solid blue, green, red and violet controls use white text.
- Amber controls use dark text because white is not readable enough on a bright amber surface.
- Fields use white backgrounds, dark text and visible grey borders.
- Disabled controls remain readable instead of fading to near-invisible colours.

The local script checks all approved foreground/background pairs against a 4.5:1 normal-text target and scans the frontend source for legacy light-on-light utility combinations that are normalized by `src/styles/contrast-system.css`.

Run from `frontend/civichero-web`:

```powershell
node scripts/check-contrast.mjs
```
