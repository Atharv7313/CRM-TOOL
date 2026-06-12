FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY Shared/Shared.csproj Shared/
COPY AuditService/AuditService.csproj AuditService/
RUN dotnet restore AuditService/AuditService.csproj

COPY Shared/ Shared/
COPY AuditService/ AuditService/
WORKDIR /src/AuditService
RUN dotnet publish AuditService.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AuditService.dll"]
