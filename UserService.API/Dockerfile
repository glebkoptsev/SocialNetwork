FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["UserService.API/UserService.API.csproj", "UserService.API/"]
COPY ["UserService.Database/UserService.Database.csproj", "UserService.Database/"]
COPY ["Libraries.Kafka/Libraries.Kafka.csproj", "Libraries.Kafka/"]
COPY ["Libraries.NpgsqlService/Libraries.NpgsqlService.csproj", "Libraries.NpgsqlService/"]

RUN dotnet restore "UserService.API/UserService.API.csproj"
COPY . .
WORKDIR "/src/UserService.API"
RUN dotnet build "UserService.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "UserService.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UserService.API.dll"]