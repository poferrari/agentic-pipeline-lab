# =============================================================================
# Stage 1: Build & Publish (.NET 9 SDK)
# =============================================================================
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy solution and project files first for optimal layer caching
COPY src/backend/Directory.Build.props ./src/backend/
COPY src/backend/MarketPulse.sln ./src/backend/
COPY src/backend/src/App.Domain/App.Domain.csproj ./src/backend/src/App.Domain/
COPY src/backend/src/App.Application/App.Application.csproj ./src/backend/src/App.Application/
COPY src/backend/src/App.Infrastructure/App.Infrastructure.csproj ./src/backend/src/App.Infrastructure/
COPY src/backend/src/App.Api/App.Api.csproj ./src/backend/src/App.Api/
COPY src/backend/tests/App.UnitTests/App.UnitTests.csproj ./src/backend/tests/App.UnitTests/
COPY src/backend/tests/App.ArchTests/App.ArchTests.csproj ./src/backend/tests/App.ArchTests/
COPY src/backend/tests/App.IntegrationTests/App.IntegrationTests.csproj ./src/backend/tests/App.IntegrationTests/

RUN dotnet restore ./src/backend/MarketPulse.sln

# Copy full backend source and publish with strict Release optimizations
COPY src/backend/ ./src/backend/
RUN dotnet publish ./src/backend/src/App.Api/App.Api.csproj \
    --configuration Release \
    --no-restore \
    -o /app/publish \
    /p:UseAppHost=false

# =============================================================================
# Stage 2: Production Hardened Runtime (Non-Root, Port 8080 for Render)
# =============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

# Install icu-libs and tzdata for full globalization and curl for health probes
USER root
RUN apk add --no-cache icu-libs tzdata curl

# Security Hardening: Run as non-root user ($APP_UID provided by official .NET images)
USER $APP_UID

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    PORT=8080 \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "App.Api.dll"]
