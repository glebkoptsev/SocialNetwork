FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["UserService.CacheUpdateService/UserService.CacheUpdateService.csproj", "UserService.CacheUpdateService/"]
COPY ["UserService.Database/UserService.Database.csproj", "UserService.Database/"]
COPY ["Libraries.Kafka/Libraries.Kafka.csproj", "Libraries.Kafka/"]
COPY ["Libraries.NpgsqlService/Libraries.NpgsqlService.csproj", "Libraries.NpgsqlService/"]
COPY ["Libraries.Clients.Common/Libraries.Clients.Common.csproj", "Libraries.Clients.Common/"]
RUN dotnet restore "./UserService.CacheUpdateService/UserService.CacheUpdateService.csproj"
COPY . .
WORKDIR "/src/UserService.CacheUpdateService"
RUN dotnet build "./UserService.CacheUpdateService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./UserService.CacheUpdateService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UserService.CacheUpdateService.dll"]