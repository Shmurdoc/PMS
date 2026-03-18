# ═══════════════════════════════════════════════════════════════════════
#  SAFARIstack PMS — Production Multi-Stage Dockerfile
#  .NET 9.0 | PostgreSQL 18 | Alpine-slim for minimal attack surface
# ═══════════════════════════════════════════════════════════════════════

# ─── Stage 1: Restore & Build ─────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy solution and project files first (layer caching for NuGet restore)
COPY global.json ./
COPY src/SAFARIstack.API/SAFARIstack.API.csproj src/SAFARIstack.API/
COPY src/SAFARIstack.Core/SAFARIstack.Core.csproj src/SAFARIstack.Core/
COPY src/SAFARIstack.Infrastructure/SAFARIstack.Infrastructure.csproj src/SAFARIstack.Infrastructure/
COPY src/SAFARIstack.Shared/SAFARIstack.Shared.csproj src/SAFARIstack.Shared/
COPY src/SAFARIstack.Modules.Staff/SAFARIstack.Modules.Staff.csproj src/SAFARIstack.Modules.Staff/
COPY src/SAFARIstack.Modules.Addons/SAFARIstack.Modules.Addons.csproj src/SAFARIstack.Modules.Addons/
COPY src/SAFARIstack.Modules.Analytics/SAFARIstack.Modules.Analytics.csproj src/SAFARIstack.Modules.Analytics/
COPY src/SAFARIstack.Modules.Channels/SAFARIstack.Modules.Channels.csproj src/SAFARIstack.Modules.Channels/
COPY src/SAFARIstack.Modules.Events/SAFARIstack.Modules.Events.csproj src/SAFARIstack.Modules.Events/
COPY src/SAFARIstack.Modules.Revenue/SAFARIstack.Modules.Revenue.csproj src/SAFARIstack.Modules.Revenue/

RUN dotnet restore src/SAFARIstack.API/SAFARIstack.API.csproj

# Copy full source and publish
COPY src/ src/
RUN dotnet publish src/SAFARIstack.API/SAFARIstack.API.csproj \
    -c Release \
    -o /app/publish \
    /p:DebugType=None \
    /p:DebugSymbols=false

# ─── Stage 2: Runtime (minimal image, non-root user) ──────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

# Security: Create non-root user
RUN addgroup -S safaristack && adduser -S safaristack -G safaristack

# Create log directory with correct ownership
RUN mkdir -p /var/log/safaristack && chown -R safaristack:safaristack /var/log/safaristack

# Copy published output
COPY --from=build /app/publish .

# Switch to non-root user
USER safaristack

# Environment configuration
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_EnableDiagnostics=0

EXPOSE 8080

# Health check built into container
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "SAFARIstack.API.dll"]
