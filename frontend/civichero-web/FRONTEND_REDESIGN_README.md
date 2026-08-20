# CivicHero Frontend UI Redesign

## Purpose

This frontend redesign changes CivicHero from a dark, technical-looking interface into a clean, modern and user-friendly civic portal based on the supplied wireframes.

Only the React frontend was redesigned. The ASP.NET Core backend, AWS RDS database, Amazon S3 integration, APIs and database tables were not changed.

---

## Main improvements

- Modern light civic theme using blue, white, green, amber and red status colours
- Responsive public landing page
- Redesigned login and citizen registration screens
- Modern role-based portal layout with desktop sidebar and mobile navigation
- Citizen dashboard with summary, quick actions, complaints, timeline, nearby issues and rewards
- Complaint reporting page with clear form sections, image upload, location and duplicate guidance
- Common issues and upvote experience
- Complaint details and timeline views
- Heatmap, leaderboard and rewards pages
- Officer dashboard and resolution upload workflow
- Supervisor dispute-review dashboard
- Admin analytics, complaint and user-management dashboard
- Reusable cards, badges, notifications, confirmation popups, header and footer
- Better spacing, typography, focus states and mobile behaviour

---

## Important paths

```text
CivicHeroSolution/
└── frontend/
    └── civichero-web/
        ├── src/
        │   ├── components/
        │   ├── layouts/
        │   ├── pages/
        │   ├── routes/
        │   └── index.css
        ├── .env.example
        ├── package.json
        └── vite.config.js
```

---

## Requirements on the laptop

Install:

1. Node.js 20 LTS or newer
2. npm
3. Visual Studio Code
4. Git
5. The CivicHero ASP.NET Core backend running at `http://localhost:5180`

Check the installations:

```powershell
node --version
npm --version
git --version
```

---

## First-time setup

Open PowerShell:

```powershell
cd "C:\CivicHeroDev\CivicHeroSolution\frontend\civichero-web"
```

Create the local environment file:

```powershell
Copy-Item ".env.example" ".env"
```

Install frontend dependencies:

```powershell
npm install
```

Start the frontend:

```powershell
npm run dev
```

Open:

```text
http://localhost:5173
```

The Vite proxy sends `/api`, `/health` and `/hubs` requests to:

```text
http://localhost:5180
```

---

## Start backend and frontend together

### PowerShell window 1 — backend

```powershell
cd "C:\CivicHeroDev\CivicHeroSolution\backend\CivicHero.Backend"
dotnet run --launch-profile http
```

### PowerShell window 2 — frontend

```powershell
cd "C:\CivicHeroDev\CivicHeroSolution\frontend\civichero-web"
npm install
npm run dev
```

---

## Production build test

```powershell
cd "C:\CivicHeroDev\CivicHeroSolution\frontend\civichero-web"
npm run build
```

A successful build creates:

```text
dist/
```

Preview the production build:

```powershell
npm run preview
```

---

## Main redesigned routes

| Screen | Route |
|---|---|
| Home | `/` |
| Login | `/login` |
| Register | `/register` |
| Forgot password | `/forgot-password` |
| Citizen dashboard | `/citizen/dashboard` |
| Report complaint | `/citizen/report` |
| My complaints | `/citizen/complaints` |
| Common issues | `/citizen/common-issues` |
| Complaint details | `/citizen/complaints/:id` |
| Complaint timeline | `/citizen/complaints/:id/timeline` |
| City heatmap | `/citizen/heatmap` |
| Leaderboard | `/citizen/leaderboard` |
| Rewards | `/citizen/rewards` |
| Officer dashboard | `/officer/dashboard` |
| Upload resolution | `/officer/assignments/:id` |
| Supervisor dashboard | `/supervisor/dashboard` |
| Admin dashboard | `/admin/dashboard` |

---

## Safe replacement method

Before replacing your current frontend, make a backup:

```powershell
cd "C:\CivicHeroDev\CivicHeroSolution\frontend"
Rename-Item "civichero-web" "civichero-web-backup"
```

Extract the downloaded complete frontend ZIP into:

```text
C:\CivicHeroDev\CivicHeroSolution\frontend\
```

Then create `.env`, install dependencies and start Vite.

Do not copy an old `node_modules` folder. Always run `npm install` on the target laptop.

---

## Patch-only method

The patch ZIP contains only redesigned and newly added files.

1. Back up the existing frontend.
2. Extract the patch at the project root.
3. Allow Windows to replace matching files.
4. Run:

```powershell
cd "C:\CivicHeroDev\CivicHeroSolution\frontend\civichero-web"
npm install
npm run build
npm run dev
```

---

## Files intentionally excluded from the complete ZIP

For security and cleanliness, the package excludes:

- `.env`
- `.env.local`
- `.env.production`
- `.env.staging`
- `node_modules`
- `dist`
- coverage reports
- Playwright test output
- local cache directories

Use `.env.example` to create your own `.env`.

---

## Validation completed

- All modified JSX/JavaScript files passed a TypeScript parser syntax check.
- Every relative import in `src` was checked and resolves to an existing file.
- Routes were checked against their imported page components.
- Secret environment files were excluded from the downloadable ZIP.

The full Vite build could not be completed inside the packaging environment because the npm registry was unavailable for dependency installation. Run `npm install` followed by `npm run build` on the development laptop before merging.

---

## Design notes

The redesign follows these principles:

- User actions are visible within one to three clicks.
- Citizen, Officer, Supervisor and Admin interfaces remain separate.
- Statuses use consistent colour-coded badges.
- Forms include labels, clear grouping and responsive layouts.
- Tables collapse into readable mobile cards where necessary.
- Confirmation messages appear after important actions.
- Existing service and API modules remain the source of live data.

---

## No database changes

This package does **not** create, remove, rename or modify any database table. It is a frontend-only change.
