FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
RUN apt-get update && apt-get install -y libkrb5-3 && rm -rf /var/lib/apt/lists/*
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["UserService.PostsGenerator/UserService.PostsGenerator.csproj", "UserService.PostsGenerator/"]
COPY ["UserService.Database/UserService.Database.csproj", "UserService.Database/"]
RUN dotnet restore "./UserService.PostsGenerator/UserService.PostsGenerator.csproj"
COPY . .
WORKDIR "/src/UserService.PostsGenerator"
RUN dotnet build "./UserService.PostsGenerator.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./UserService.PostsGenerator.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UserService.PostsGenerator.dll"]
