# ── Build Stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore (layer cache friendly)
COPY AsistanApp.csproj ./
RUN dotnet restore

# Copy everything else and publish
COPY . .
RUN dotnet publish AsistanApp.csproj -c Release -o /app/publish --no-restore

# ── Runtime Stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# SQLite database will live in /data (Railway persistent volume or ephemeral)
RUN mkdir -p /data

COPY --from=build /app/publish .

# Railway injects PORT; ASP.NET Core reads ASPNETCORE_URLS
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DEFAULT_CONNECTION="Data Source=/data/asistanapp.db"

EXPOSE 8080

ENTRYPOINT ["dotnet", "AsistanApp.dll"]
