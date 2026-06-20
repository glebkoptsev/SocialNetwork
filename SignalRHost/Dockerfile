FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["SignalRHost/SignalRHost.csproj", "SignalRHost/"]
COPY ["Libraries.Web.Common/Libraries.Web.Common.csproj", "Libraries.Web.Common/"]
RUN dotnet restore "SignalRHost/SignalRHost.csproj"
COPY . .
WORKDIR "/src/SignalRHost"
RUN dotnet build "SignalRHost.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SignalRHost.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SignalRHost.dll"]
