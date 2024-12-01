# syntax=docker.io/docker/dockerfile:1.7-labs

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /build

ARG CONFIG

ENV DOTNET_NOLOGO=true
ENV DOTNET_CLI_TELEMETRY_OPTOUT=true

COPY --exclude=Tools SRC .

RUN dotnet build Warehouse.API/Warehouse.API.csproj -c $CONFIG -o ./bin

# ---------------------------------------------------

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 1986

RUN apt-get update && apt-get install -y curl

COPY --from=build /build/bin .

ENV DOTNET_NOLOGO=true
ENV DOTNET_CLI_TELEMETRY_OPTOUT=true

LABEL author="Denes Solti" title="Warehouse.API"

HEALTHCHECK --interval=5m --timeout=3s --start-period=10s --retries=1 CMD curl --fail --insecure https://localhost:1986/api/v1/healthcheck || exit 1

ENTRYPOINT ["dotnet", "Warehouse.API.dll"]