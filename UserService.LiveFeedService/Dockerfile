FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["UserService.LiveFeedService/UserService.LiveFeedService.csproj", "UserService.LiveFeedService/"]
COPY ["Libraries.Web.Common/Libraries.Web.Common.csproj", "Libraries.Web.Common/"]
RUN dotnet restore "./UserService.LiveFeedService/UserService.LiveFeedService.csproj"
COPY . .
WORKDIR "/src/UserService.LiveFeedService"
RUN dotnet build "./UserService.LiveFeedService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./UserService.LiveFeedService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UserService.LiveFeedService.dll"]