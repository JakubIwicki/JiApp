FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/JiApp.ImageTools/JiApp.ImageTools.csproj", "src/JiApp.ImageTools/"]
COPY ["src/JiApp.Common/JiApp.Common.csproj", "src/JiApp.Common/"]
COPY Directory.Build.props .
RUN dotnet restore "src/JiApp.ImageTools/JiApp.ImageTools.csproj"
COPY . .
WORKDIR "/src/src/JiApp.ImageTools"
RUN dotnet build "JiApp.ImageTools.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "JiApp.ImageTools.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "JiApp.ImageTools.dll"]
