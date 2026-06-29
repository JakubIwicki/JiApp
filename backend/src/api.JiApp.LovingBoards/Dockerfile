FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/api.JiApp.LovingBoards/api.JiApp.LovingBoards.csproj", "src/api.JiApp.LovingBoards/"]
COPY ["src/JiApp.Common/JiApp.Common.csproj", "src/JiApp.Common/"]
COPY Directory.Build.props .
RUN dotnet restore "src/api.JiApp.LovingBoards/api.JiApp.LovingBoards.csproj"
COPY . .
WORKDIR "/src/src/api.JiApp.LovingBoards"
RUN dotnet build "api.JiApp.LovingBoards.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "api.JiApp.LovingBoards.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "api.JiApp.LovingBoards.dll"]
