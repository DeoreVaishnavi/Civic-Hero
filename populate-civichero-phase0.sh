#!/usr/bin/env bash
set -euo pipefail

ROOT="$PWD/CivicHeroSolution"
FORCE="false"

for arg in "$@"; do
  case "$arg" in
    --force) FORCE="true" ;;
    *) ROOT="$arg" ;;
  esac
done

ROOT="$(mkdir -p "$ROOT" && cd "$ROOT" && pwd)"

write_file() {
  local relative_path="$1"
  local target="$ROOT/$relative_path"

  mkdir -p "$(dirname "$target")"

  if [[ -s "$target" && "$FORCE" != "true" ]]; then
    cat >/dev/null
    printf 'SKIP  %s (already has content)\n' "$relative_path"
    return
  fi

  cat > "$target"
  printf 'WRITE %s\n' "$relative_path"
}

echo "Populating CivicHero Phase 0 baseline at:"
echo "$ROOT"
echo

# -----------------------------------------------------------------------------
# Root files
# -----------------------------------------------------------------------------
write_file ".gitignore" <<'EOF'
# .NET
**/bin/
**/obj/
*.user
*.suo
.vs/

# Node / Vite
**/node_modules/
**/dist/
**/.vite/

# Environment and secrets
.env.local
.env.*.local
**/appsettings.Local.json
**/appsettings.Secrets.json

# IDE / OS
.idea/
.vscode/
.DS_Store
Thumbs.db

# Logs
*.log
logs/

# Test output
TestResults/
coverage/
playwright-report/
EOF

write_file "README.md" <<'EOF'
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
EOF

# -----------------------------------------------------------------------------
# Backend project
# -----------------------------------------------------------------------------
write_file "backend/CivicHero.Backend/CivicHero.Backend.csproj" <<'EOF'
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>CivicHero.Backend</RootNamespace>
    <AssemblyName>CivicHero.Backend</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
  </ItemGroup>
</Project>
EOF

# Keep the blueprint's outer backend project buildable without compiling nested files twice.
write_file "backend/backend.csproj" <<'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="CivicHero.Backend/CivicHero.Backend.csproj" />
  </ItemGroup>
</Project>
EOF

write_file "backend/Program.cs" <<'EOF'
// The runnable ASP.NET Core application is backend/CivicHero.Backend/Program.cs.
// This file is intentionally excluded by backend.csproj.
EOF

write_file "backend/appsettings.json" <<'EOF'
{}
EOF

write_file "backend/appsettings.Development.json" <<'EOF'
{}
EOF

write_file "backend/backend.http" <<'EOF'
@CivicHeroApi = http://localhost:5180

GET {{CivicHeroApi}}/api/v1/health
Accept: application/json
EOF

write_file "backend/CivicHero.Backend/Program.cs" <<'EOF'
using CivicHero.Backend.Infrastructure.Extensions;
using CivicHero.Backend.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCivicHeroServices(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "CivicHero API v1");
        options.RoutePrefix = "swagger";
    });
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors("CivicHeroFrontend");
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health/live");

app.Run();

public partial class Program;
EOF

write_file "backend/CivicHero.Backend/Infrastructure/Extensions/ServiceCollectionExtensions.cs" <<'EOF'
namespace CivicHero.Backend.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCivicHeroServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddProblemDetails();
        services.AddHealthChecks();

        var configuredOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .GetChildren()
            .Select(section => section.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToArray();

        var allowedOrigins = configuredOrigins.Length > 0
            ? configuredOrigins
            : ["http://localhost:5173"];

        services.AddCors(options =>
        {
            options.AddPolicy("CivicHeroFrontend", policy =>
            {
                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }
}
EOF

write_file "backend/CivicHero.Backend/Middleware/CorrelationIdMiddleware.cs" <<'EOF'
namespace CivicHero.Backend.Middleware;

public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var suppliedId)
            && !string.IsNullOrWhiteSpace(suppliedId)
                ? suppliedId.ToString()
                : Guid.NewGuid().ToString("N");

        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        await _next(context);
    }
}
EOF

write_file "backend/CivicHero.Backend/Middleware/GlobalExceptionMiddleware.cs" <<'EOF'
using System.Net;
using System.Text.Json;

namespace CivicHero.Backend.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unhandled exception. CorrelationId: {CorrelationId}",
                context.TraceIdentifier);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var response = new
            {
                success = false,
                message = "An unexpected error occurred.",
                errors = Array.Empty<string>(),
                traceId = context.TraceIdentifier
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
        }
    }
}
EOF

write_file "backend/CivicHero.Backend/Controllers/HealthController.cs" <<'EOF'
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/health")]
public sealed class HealthController : ControllerBase
{
    private readonly IHostEnvironment _environment;

    public HealthController(IHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            success = true,
            message = "CivicHero API is running",
            data = new
            {
                status = "Healthy",
                service = "CivicHero.Backend",
                version = "1.0.0-phase0",
                environment = _environment.EnvironmentName,
                serverTimeUtc = DateTimeOffset.UtcNow
            }
        });
    }
}
EOF

write_file "backend/CivicHero.Backend/appsettings.json" <<'EOF'
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:5173"
    ]
  }
}
EOF

write_file "backend/CivicHero.Backend/appsettings.Development.json" <<'EOF'
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Information"
    }
  }
}
EOF

write_file "backend/CivicHero.Backend/appsettings.Staging.json" <<'EOF'
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
EOF

write_file "backend/CivicHero.Backend/appsettings.Production.json" <<'EOF'
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
EOF

write_file "backend/CivicHero.Backend/CivicHero.Backend.http" <<'EOF'
@CivicHeroApi = http://localhost:5180

### API health
GET {{CivicHeroApi}}/api/v1/health
Accept: application/json

### ASP.NET Core liveness health check
GET {{CivicHeroApi}}/health/live
Accept: application/json
EOF

write_file "backend/CivicHero.Backend/Properties/launchSettings.json" <<'EOF'
{
  "$schema": "http://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "http://localhost:5180",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "https://localhost:7180;http://localhost:5180",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
EOF

# -----------------------------------------------------------------------------
# Frontend project
# -----------------------------------------------------------------------------
write_file "frontend/civichero-web/package.json" <<'EOF'
{
  "name": "civichero-web",
  "private": true,
  "version": "1.0.0-phase0",
  "type": "module",
  "engines": {
    "node": ">=20"
  },
  "scripts": {
    "dev": "vite",
    "build": "vite build",
    "preview": "vite preview"
  },
  "dependencies": {
    "axios": "^1.7.9",
    "react": "^19.0.0",
    "react-dom": "^19.0.0",
    "react-router-dom": "^7.1.5",
    "zustand": "^5.0.3"
  },
  "devDependencies": {
    "@vitejs/plugin-react": "^4.3.4",
    "autoprefixer": "^10.4.20",
    "postcss": "^8.5.1",
    "tailwindcss": "^3.4.17",
    "vite": "^6.1.0"
  }
}
EOF

write_file "frontend/civichero-web/index.html" <<'EOF'
<!doctype html>
<html lang="en">
  <head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <meta
      name="description"
      content="CivicHero civic complaint governance platform"
    />
    <title>CivicHero</title>
  </head>
  <body class="bg-slate-950">
    <div id="root"></div>
    <script type="module" src="/src/main.jsx"></script>
  </body>
</html>
EOF

write_file "frontend/civichero-web/vite.config.js" <<'EOF'
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      '/api': {
        target: 'http://localhost:5180',
        changeOrigin: true,
      },
      '/health': {
        target: 'http://localhost:5180',
        changeOrigin: true,
      },
    },
  },
});
EOF

write_file "frontend/civichero-web/postcss.config.js" <<'EOF'
export default {
  plugins: {
    tailwindcss: {},
    autoprefixer: {},
  },
};
EOF

write_file "frontend/civichero-web/tailwind.config.js" <<'EOF'
/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      fontFamily: {
        sans: ['Inter', 'ui-sans-serif', 'system-ui', 'sans-serif'],
      },
    },
  },
  plugins: [],
};
EOF

write_file "frontend/civichero-web/.env" <<'EOF'
VITE_API_BASE_URL=/api/v1
EOF

write_file "frontend/civichero-web/.env.example" <<'EOF'
VITE_API_BASE_URL=/api/v1
EOF

write_file "frontend/civichero-web/.env.staging" <<'EOF'
VITE_API_BASE_URL=/api/v1
EOF

write_file "frontend/civichero-web/.env.production" <<'EOF'
VITE_API_BASE_URL=/api/v1
EOF

write_file "frontend/civichero-web/src/main.jsx" <<'EOF'
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import App from './App.jsx';
import './index.css';

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <BrowserRouter>
      <App />
    </BrowserRouter>
  </StrictMode>,
);
EOF

write_file "frontend/civichero-web/src/App.jsx" <<'EOF'
import Footer from './components/layout/Footer.jsx';
import Header from './components/layout/Header.jsx';
import AppRoutes from './routes/AppRoutes.jsx';

export default function App() {
  return (
    <div className="flex min-h-screen flex-col bg-slate-950 text-slate-100">
      <Header />
      <main className="flex-1">
        <AppRoutes />
      </main>
      <Footer />
    </div>
  );
}
EOF

write_file "frontend/civichero-web/src/index.css" <<'EOF'
@tailwind base;
@tailwind components;
@tailwind utilities;

:root {
  color-scheme: dark;
  font-family: Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont,
    "Segoe UI", sans-serif;
  font-synthesis: none;
  text-rendering: optimizeLegibility;
}

* {
  box-sizing: border-box;
}

body {
  margin: 0;
  min-width: 320px;
  min-height: 100vh;
}

button,
a {
  -webkit-tap-highlight-color: transparent;
}
EOF

write_file "frontend/civichero-web/src/api/axiosInstance.js" <<'EOF'
import axios from 'axios';

const axiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api/v1',
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
  },
});

axiosInstance.interceptors.response.use(
  (response) => response,
  (error) => {
    const normalizedError = {
      status: error.response?.status ?? 0,
      message:
        error.response?.data?.message ||
        error.message ||
        'Unable to communicate with the CivicHero API.',
      traceId: error.response?.data?.traceId ?? null,
      originalError: error,
    };

    return Promise.reject(normalizedError);
  },
);

export default axiosInstance;
EOF

write_file "frontend/civichero-web/src/api/tokenRefreshHandler.js" <<'EOF'
// Refresh-token handling is implemented in the authentication phase.
export function configureTokenRefresh() {
  return undefined;
}
EOF

write_file "frontend/civichero-web/src/services/healthApi.js" <<'EOF'
import axiosInstance from '../api/axiosInstance.js';

export async function getApiHealth() {
  const response = await axiosInstance.get('/health');
  return response.data;
}
EOF

write_file "frontend/civichero-web/src/components/common/ApiStatus.jsx" <<'EOF'
const STATUS_STYLES = {
  checking: 'border-amber-400/30 bg-amber-400/10 text-amber-200',
  connected: 'border-emerald-400/30 bg-emerald-400/10 text-emerald-200',
  unavailable: 'border-rose-400/30 bg-rose-400/10 text-rose-200',
};

const STATUS_DOTS = {
  checking: 'bg-amber-300 animate-pulse',
  connected: 'bg-emerald-300',
  unavailable: 'bg-rose-300',
};

export default function ApiStatus({ status, message }) {
  return (
    <div
      className={`inline-flex items-center gap-3 rounded-full border px-4 py-2 text-sm font-semibold ${STATUS_STYLES[status]}`}
      role="status"
      aria-live="polite"
    >
      <span className={`h-2.5 w-2.5 rounded-full ${STATUS_DOTS[status]}`} />
      <span>{message}</span>
    </div>
  );
}
EOF

write_file "frontend/civichero-web/src/components/layout/Header.jsx" <<'EOF'
export default function Header() {
  return (
    <header className="border-b border-white/10 bg-slate-950/80 backdrop-blur">
      <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
        <a href="/" className="text-xl font-black tracking-tight text-white">
          Civic<span className="text-sky-400">Hero</span>
        </a>
        <span className="rounded-full border border-sky-400/20 bg-sky-400/10 px-3 py-1 text-xs font-semibold text-sky-200">
          Phase 0
        </span>
      </div>
    </header>
  );
}
EOF

write_file "frontend/civichero-web/src/components/layout/Footer.jsx" <<'EOF'
export default function Footer() {
  return (
    <footer className="border-t border-white/10 px-6 py-6 text-center text-sm text-slate-500">
      CivicHero enterprise technical baseline
    </footer>
  );
}
EOF

write_file "frontend/civichero-web/src/pages/public/HomePage.jsx" <<'EOF'
import { useCallback, useEffect, useState } from 'react';
import ApiStatus from '../../components/common/ApiStatus.jsx';
import { getApiHealth } from '../../services/healthApi.js';

export default function HomePage() {
  const [status, setStatus] = useState('checking');
  const [health, setHealth] = useState(null);
  const [errorMessage, setErrorMessage] = useState('');

  const checkHealth = useCallback(async () => {
    setStatus('checking');
    setErrorMessage('');

    try {
      const response = await getApiHealth();
      setHealth(response.data);
      setStatus('connected');
    } catch (error) {
      setHealth(null);
      setErrorMessage(error.message);
      setStatus('unavailable');
    }
  }, []);

  useEffect(() => {
    checkHealth();
  }, [checkHealth]);

  const statusMessage = {
    checking: 'Checking CivicHero API…',
    connected: 'CivicHero API: Connected',
    unavailable: 'CivicHero API: Unavailable',
  }[status];

  return (
    <section className="relative overflow-hidden px-6 py-20 sm:py-28">
      <div className="absolute inset-0 -z-10 bg-[radial-gradient(circle_at_top_left,rgba(14,165,233,0.18),transparent_38%),radial-gradient(circle_at_bottom_right,rgba(16,185,129,0.12),transparent_35%)]" />

      <div className="mx-auto max-w-6xl">
        <div className="max-w-3xl">
          <ApiStatus status={status} message={statusMessage} />

          <h1 className="mt-8 text-5xl font-black tracking-tight text-white sm:text-7xl">
            Better civic issues.
            <span className="block text-sky-400">Faster public action.</span>
          </h1>

          <p className="mt-6 max-w-2xl text-lg leading-8 text-slate-300">
            CivicHero connects citizens, officers, supervisors, and administrators
            through a transparent complaint-resolution workflow.
          </p>

          <div className="mt-10 flex flex-wrap gap-4">
            <button
              type="button"
              onClick={checkHealth}
              disabled={status === 'checking'}
              className="rounded-xl bg-sky-500 px-5 py-3 font-bold text-white transition hover:bg-sky-400 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {status === 'checking' ? 'Checking…' : 'Check API again'}
            </button>

            <a
              href="http://localhost:5180/swagger"
              target="_blank"
              rel="noreferrer"
              className="rounded-xl border border-white/15 px-5 py-3 font-bold text-slate-200 transition hover:border-white/30 hover:bg-white/5"
            >
              Open Swagger
            </a>
          </div>
        </div>

        <div className="mt-14 grid gap-4 sm:grid-cols-3">
          <InfoCard label="Frontend" value="React 19 + Vite 6" />
          <InfoCard label="Backend" value={health?.service || 'ASP.NET Core 8'} />
          <InfoCard label="Environment" value={health?.environment || 'Development'} />
        </div>

        {status === 'unavailable' && (
          <div className="mt-6 rounded-2xl border border-rose-400/20 bg-rose-400/10 p-4 text-sm text-rose-100">
            <strong>Connection error:</strong> {errorMessage}
            <div className="mt-2 text-rose-200/80">
              Start the backend at http://localhost:5180, then retry.
            </div>
          </div>
        )}
      </div>
    </section>
  );
}

function InfoCard({ label, value }) {
  return (
    <div className="rounded-2xl border border-white/10 bg-white/[0.04] p-5 shadow-2xl shadow-black/10">
      <div className="text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
        {label}
      </div>
      <div className="mt-2 text-lg font-bold text-white">{value}</div>
    </div>
  );
}
EOF

write_file "frontend/civichero-web/src/routes/routePaths.js" <<'EOF'
export const ROUTE_PATHS = Object.freeze({
  home: '/',
});
EOF

write_file "frontend/civichero-web/src/routes/AppRoutes.jsx" <<'EOF'
import { Navigate, Route, Routes } from 'react-router-dom';
import HomePage from '../pages/public/HomePage.jsx';
import { ROUTE_PATHS } from './routePaths.js';

export default function AppRoutes() {
  return (
    <Routes>
      <Route path={ROUTE_PATHS.home} element={<HomePage />} />
      <Route path="*" element={<Navigate to={ROUTE_PATHS.home} replace />} />
    </Routes>
  );
}
EOF

# -----------------------------------------------------------------------------
# Baseline GitHub Actions
# -----------------------------------------------------------------------------
write_file ".github/workflows/backend-build.yml" <<'EOF'
name: Backend Build

on:
  push:
    paths:
      - 'backend/**'
      - '.github/workflows/backend-build.yml'
  pull_request:
    paths:
      - 'backend/**'

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - name: Restore
        run: dotnet restore backend/CivicHero.Backend/CivicHero.Backend.csproj
      - name: Build
        run: dotnet build backend/CivicHero.Backend/CivicHero.Backend.csproj --configuration Release --no-restore
EOF

write_file ".github/workflows/frontend-build.yml" <<'EOF'
name: Frontend Build

on:
  push:
    paths:
      - 'frontend/**'
      - '.github/workflows/frontend-build.yml'
  pull_request:
    paths:
      - 'frontend/**'

jobs:
  build:
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: frontend/civichero-web
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: '20'
          cache: 'npm'
          cache-dependency-path: frontend/civichero-web/package-lock.json
      - name: Install dependencies
        run: npm install
      - name: Build
        run: npm run build
EOF

# -----------------------------------------------------------------------------
# Create a solution containing only the runnable backend project.
# -----------------------------------------------------------------------------
if command -v dotnet >/dev/null 2>&1; then
  (
    cd "$ROOT"
    if [[ ! -f "CivicHeroSolution.sln" ]]; then
      dotnet new sln -n CivicHeroSolution >/dev/null
      echo "WRITE CivicHeroSolution.sln"
    fi

    dotnet sln CivicHeroSolution.sln add \
      backend/CivicHero.Backend/CivicHero.Backend.csproj >/dev/null 2>&1 || true
  )
else
  echo "NOTE  dotnet was not found; create the solution later with:"
  echo "      dotnet new sln -n CivicHeroSolution"
  echo "      dotnet sln CivicHeroSolution.sln add backend/CivicHero.Backend/CivicHero.Backend.csproj"
fi

echo
echo "Phase 0 baseline files are ready."
echo
echo "Run backend:"
echo "  cd \"$ROOT/backend/CivicHero.Backend\""
echo "  dotnet restore"
echo "  dotnet run --launch-profile http"
echo
echo "Run frontend in a second terminal:"
echo "  cd \"$ROOT/frontend/civichero-web\""
echo "  npm install"
echo "  npm run dev"
echo
echo "Open: http://localhost:5173"
echo "Swagger: http://localhost:5180/swagger"
echo
echo "Use --force only when you intentionally want to overwrite these baseline files."
