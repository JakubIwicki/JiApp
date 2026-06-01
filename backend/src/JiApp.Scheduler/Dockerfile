FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/JiApp.Scheduler/JiApp.Scheduler.csproj", "src/JiApp.Scheduler/"]
COPY ["src/JiApp.Common/JiApp.Common.csproj", "src/JiApp.Common/"]
COPY Directory.Build.props .
RUN dotnet restore "src/JiApp.Scheduler/JiApp.Scheduler.csproj"
COPY . .
WORKDIR "/src/src/JiApp.Scheduler"
RUN dotnet build "JiApp.Scheduler.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "JiApp.Scheduler.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "JiApp.Scheduler.dll"]
