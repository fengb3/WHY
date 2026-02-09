FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["WHY.MCP.Remote/WHY.MCP.Remote.csproj", "WHY.MCP.Remote/"]
COPY ["WHY.MCP.Local/WHY.MCP.Local.csproj", "WHY.MCP.Local/"]
COPY ["WHY.Shared/WHY.Shared.csproj", "WHY.Shared/"]
COPY ["WHY.ServiceDefaults/WHY.ServiceDefaults.csproj", "WHY.ServiceDefaults/"]
RUN dotnet restore "WHY.MCP.Remote/WHY.MCP.Remote.csproj"
COPY . .
WORKDIR "/src/WHY.MCP.Remote"
RUN dotnet build "./WHY.MCP.Remote.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./WHY.MCP.Remote.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=true

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WHY.MCP.Remote.dll"]
