FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY Shared/Shared.csproj Shared/
COPY AuditLogService.API/AuditLogService.API.csproj AuditLogService.API/
RUN dotnet restore AuditLogService.API/AuditLogService.API.csproj

COPY Shared/ Shared/
COPY AuditLogService.API/ AuditLogService.API/
WORKDIR /src/AuditLogService.API
RUN dotnet publish AuditLogService.API.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AuditLogService.API.dll"]
