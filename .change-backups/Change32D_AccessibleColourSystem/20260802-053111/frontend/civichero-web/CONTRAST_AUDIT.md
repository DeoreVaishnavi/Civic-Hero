# CivicHero UI Contrast Audit

The authenticated portal follows a light-workspace system:

- Light canvas and cards use navy or slate text.
- Dark modals, code blocks, sidebars and saturated actions use white or very light text.
- Pastel status/alert backgrounds use dark, matching foreground colours.
- Form controls use white backgrounds, dark text and visible borders.

Audited colour pairs meet the WCAG AA target of 4.5:1 for normal text. Essential UI boundaries target at least 3:1.

Run the local palette audit from `frontend/civichero-web`:

```powershell
node scripts/check-contrast.mjs
```

The audit calculates the defined foreground/background ratios and confirms the final contrast stylesheet is present.
