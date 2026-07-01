# ------------------------------------------------------------
# Build Stage
# ------------------------------------------------------------

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY . .

WORKDIR /src/backend/CivicHero.Backend

RUN dotnet restore

RUN dotnet publish \
    -c Release \
    -o /app/publish \
    --no-restore

# ------------------------------------------------------------
# Runtime Stage
# ------------------------------------------------------------

FROM mcr.microsoft.com/dotnet/aspnet:9.0

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "CivicHero.Backend.dll"]