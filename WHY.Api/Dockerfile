FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["WHY.Api/WHY.Api.csproj", "WHY.Api/"]
COPY ["WHY.Database/WHY.Database.csproj", "WHY.Database/"]
COPY ["WHY.ServiceDefaults/WHY.ServiceDefaults.csproj", "WHY.ServiceDefaults/"]
COPY ["WHY.Shared/WHY.Shared.csproj", "WHY.Shared/"]
RUN dotnet restore "WHY.Api/WHY.Api.csproj"
COPY . .
WORKDIR "/src/WHY.Api"
RUN dotnet build "./WHY.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./WHY.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WHY.Api.dll"]
