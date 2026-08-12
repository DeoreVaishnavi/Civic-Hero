# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS restore
WORKDIR /src
COPY backend/CivicHero.Backend/CivicHero.Backend.csproj backend/CivicHero.Backend/
RUN dotnet restore backend/CivicHero.Backend/CivicHero.Backend.csproj

FROM restore AS publish
COPY backend/CivicHero.Backend/ backend/CivicHero.Backend/
RUN dotnet publish backend/CivicHero.Backend/CivicHero.Backend.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=publish --chown=app:app /app/publish ./
COPY --chown=app:app deployment/backend/docker-entrypoint.sh /usr/local/bin/civichero-entrypoint
RUN chmod 0555 /usr/local/bin/civichero-entrypoint

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080
USER app
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD curl --fail --silent --show-error http://127.0.0.1:8080/health/live || exit 1
ENTRYPOINT ["/usr/local/bin/civichero-entrypoint"]
