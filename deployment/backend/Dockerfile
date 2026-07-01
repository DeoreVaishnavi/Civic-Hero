# ==========================
# Build Stage
# ==========================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Copy entire solution
COPY . .

# Move to backend project
WORKDIR /src/backend/CivicHero.Backend

# Restore dependencies
RUN dotnet restore CivicHero.Backend.csproj

# Publish application
RUN dotnet publish CivicHero.Backend.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ==========================
# Runtime Stage
# ==========================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "CivicHero.Backend.dll"]