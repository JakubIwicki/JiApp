FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/JiApp.Identity/JiApp.Identity.csproj", "src/JiApp.Identity/"]
COPY ["src/JiApp.Common/JiApp.Common.csproj", "src/JiApp.Common/"]
COPY Directory.Build.props .
RUN dotnet restore "src/JiApp.Identity/JiApp.Identity.csproj"
COPY . .
WORKDIR "/src/src/JiApp.Identity"
RUN dotnet build "JiApp.Identity.csproj" -c Release -o /app/build
FROM build AS publish
RUN dotnet publish "JiApp.Identity.csproj" -c Release -o /app/publish
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "JiApp.Identity.dll"]
