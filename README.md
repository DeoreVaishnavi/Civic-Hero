# CivicHero

CivicHero is a civic-complaint governance platform built with ASP.NET Core 8 and React 19.

## Phase 0 baseline

- ASP.NET Core API builds and runs.
- React/Vite frontend builds and runs.
- React calls the backend health endpoint.
- Remaining feature files stay reserved for later implementation phases.

## Run backend

```bash
cd backend/CivicHero.Backend
dotnet restore
dotnet run
```

## Run frontend

```bash
cd frontend/civichero-web
npm install
npm run dev
```

Open http://localhost:5173.

Port reference and Docker/Vite conflict guidance: [`docs/PORTS.md`](docs/PORTS.md).
