FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/JiApp.Gateway/JiApp.Gateway.csproj", "src/JiApp.Gateway/"]
COPY ["src/JiApp.Common/JiApp.Common.csproj", "src/JiApp.Common/"]
COPY Directory.Build.props .
RUN dotnet restore "src/JiApp.Gateway/JiApp.Gateway.csproj"
COPY . .
WORKDIR "/src/src/JiApp.Gateway"
RUN dotnet build "JiApp.Gateway.csproj" -c Release -o /app/build
FROM build AS publish
RUN dotnet publish "JiApp.Gateway.csproj" -c Release -o /app/publish
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "JiApp.Gateway.dll"]
