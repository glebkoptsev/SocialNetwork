FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["UserService.CacheWarmup/UserService.CacheWarmup.csproj", "UserService.CacheWarmup/"]
COPY ["UserService.Database/UserService.Database.csproj", "UserService.Database/"]
RUN dotnet restore "./UserService.CacheWarmup/UserService.CacheWarmup.csproj"
COPY . .
WORKDIR "/src/UserService.CacheWarmup"
RUN dotnet build "./UserService.CacheWarmup.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./UserService.CacheWarmup.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UserService.CacheWarmup.dll"]