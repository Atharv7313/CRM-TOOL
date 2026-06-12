FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY Shared/Shared.csproj Shared/
COPY ProtocolService/ProtocolService.csproj ProtocolService/
RUN dotnet restore ProtocolService/ProtocolService.csproj

COPY Shared/ Shared/
COPY ProtocolService/ ProtocolService/
WORKDIR /src/ProtocolService
RUN dotnet publish ProtocolService.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ProtocolService.dll"]
