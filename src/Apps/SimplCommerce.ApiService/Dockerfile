# syntax=docker/dockerfile:1.7
#
# Multi-stage build for SimplCommerce.ApiService (net9.0 Minimal API).
# Build context MUST be the repo root so ProjectReferences resolve, e.g.
#   docker build -f src/Apps/SimplCommerce.ApiService/Dockerfile -t simpl/api:dev .
#

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files first for a cacheable restore. Keep the list in sync with
# SimplCommerce.ApiService.csproj's <ProjectReference> entries.
COPY global.json ./
COPY SimplCommerce.sln ./
COPY src/ServiceDefaults/ src/ServiceDefaults/
COPY src/SimplCommerce.Infrastructure/ src/SimplCommerce.Infrastructure/
COPY src/Migrations/ src/Migrations/
COPY src/Modules/ src/Modules/
COPY src/Apps/SimplCommerce.ApiService/ src/Apps/SimplCommerce.ApiService/

RUN dotnet restore src/Apps/SimplCommerce.ApiService/SimplCommerce.ApiService.csproj

RUN dotnet publish src/Apps/SimplCommerce.ApiService/SimplCommerce.ApiService.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
# Non-root user, matches the default ASP.NET Core container user.
USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_USE_POLLING_FILE_WATCHER=false
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SimplCommerce.ApiService.dll"]
