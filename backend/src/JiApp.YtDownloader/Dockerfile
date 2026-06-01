FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/JiApp.YtDownloader/JiApp.YtDownloader.csproj", "src/JiApp.YtDownloader/"]
COPY ["src/JiApp.Common/JiApp.Common.csproj", "src/JiApp.Common/"]
COPY ["src/JiApp.YtApi/JiApp.YtApi.csproj", "src/JiApp.YtApi/"]
COPY Directory.Build.props .
RUN dotnet restore "src/JiApp.YtDownloader/JiApp.YtDownloader.csproj"
COPY . .
WORKDIR "/src/src/JiApp.YtDownloader"
RUN dotnet build "JiApp.YtDownloader.csproj" -c Release -o /app/build
FROM build AS publish
RUN dotnet publish "JiApp.YtDownloader.csproj" -c Release -o /app/publish
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update && apt-get install -y python3 python3-pip ffmpeg && \
    rm -rf /var/lib/apt/lists/*
RUN pip3 install --break-system-packages yt-dlp
WORKDIR /app
COPY --from=publish /app/publish .
ENV Youtube__YtDlpPath=yt-dlp
ENV Youtube__FfmpegPath=ffmpeg
ENTRYPOINT ["dotnet", "JiApp.YtDownloader.dll"]
