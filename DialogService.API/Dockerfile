FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["DialogService.API/DialogService.API.csproj", "DialogService.API/"]
COPY ["DialogService.Database/DialogService.Database.csproj", "DialogService.Database/"]
COPY ["Libraries.NpgsqlService/Libraries.NpgsqlService.csproj", "Libraries.NpgsqlService/"]
COPY ["Libraries.Web.Common/Libraries.Web.Common.csproj", "Libraries.Web.Common/"]
RUN dotnet restore "./DialogService.API/DialogService.API.csproj"
COPY . .
WORKDIR "/src/DialogService.API"
RUN dotnet build "./DialogService.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./DialogService.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DialogService.API.dll"]