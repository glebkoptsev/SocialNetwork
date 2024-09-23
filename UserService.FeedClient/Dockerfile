FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["UserService.FeedClient/UserService.FeedClient.csproj", "UserService.FeedClient/"]
COPY ["Libraries.Clients.Common/Libraries.Clients.Common.csproj", "Libraries.Clients.Common/"]
RUN dotnet restore "./UserService.FeedClient/UserService.FeedClient.csproj"
COPY . .
WORKDIR "/src/UserService.FeedClient"
RUN dotnet build "./UserService.FeedClient.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./UserService.FeedClient.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UserService.FeedClient.dll"]