# syntax=docker/dockerfile:1.7
#
# Multi-stage build for SimplCommerce.Storefront (Blazor Web App, Interactive Auto).
# Build context MUST be the repo root:
#   docker build -f src/Apps/SimplCommerce.Storefront/Dockerfile -t simpl/storefront:dev .
#

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY global.json ./
COPY src/ServiceDefaults/ src/ServiceDefaults/
COPY src/Apps/SimplCommerce.Storefront/ src/Apps/SimplCommerce.Storefront/
COPY src/Apps/SimplCommerce.Storefront.Client/ src/Apps/SimplCommerce.Storefront.Client/

RUN dotnet restore src/Apps/SimplCommerce.Storefront/SimplCommerce.Storefront.csproj

RUN dotnet publish src/Apps/SimplCommerce.Storefront/SimplCommerce.Storefront.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SimplCommerce.Storefront.dll"]
