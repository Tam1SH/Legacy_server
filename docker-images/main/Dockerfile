FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

WORKDIR /src
COPY ["SasavnServer.csproj", "SasavnServer/"]
RUN dotnet restore "SasavnServer/SasavnServer.csproj"
WORKDIR "/src/SasavnServer"
COPY . .
RUN dotnet build "SasavnServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SasavnServer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SasavnServer.dll"]