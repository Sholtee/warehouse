FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /build

ENV DOTNET_NOLOGO=true
ENV DOTNET_CLI_TELEMETRY_OPTOUT=true

COPY SRC .

RUN dotnet build Warehouse.API/Warehouse.API.csproj -o ./bin

# ---------------------------------------------------

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 1986

RUN apt-get update && apt-get install -y curl

COPY --from=build /build/bin .

ENV DOTNET_NOLOGO=true
ENV DOTNET_CLI_TELEMETRY_OPTOUT=true

LABEL org.opencontainers.image.authors="Denes Solti" org.opencontainers.image.title="Warehouse.API"

HEALTHCHECK --interval=5m --timeout=3s --start-period=10s --retries=1 CMD curl --fail --insecure https://localhost:1986/api/v1/healthcheck || exit 1

ENTRYPOINT ["dotnet", "Warehouse.API.dll"]