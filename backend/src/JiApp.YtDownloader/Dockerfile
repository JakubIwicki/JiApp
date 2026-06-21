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
RUN apt-get update && apt-get install -y python3 python3-pip ffmpeg curl ca-certificates unzip && \
    rm -rf /var/lib/apt/lists/*
RUN pip3 install --break-system-packages "yt-dlp[default]" bgutil-ytdlp-pot-provider
RUN curl -fsSL https://deno.land/install.sh | DENO_INSTALL=/usr/local sh && deno --version
RUN printf '%s\n' '--extractor-args "youtubepot-bgutilhttp:base_url=http://bgutil-provider:4416"' > /etc/yt-dlp.conf
WORKDIR /app
COPY --from=publish /app/publish .
# To authenticate YouTube requests, mount a cookies directory
# at /app/cookies/ with a Netscape-format cookie file inside
# (e.g., /app/cookies/youtube-cookies.txt) and set
# Youtube__CookiesFile accordingly
ENV Youtube__YtDlpPath=yt-dlp
ENV Youtube__FfmpegPath=ffmpeg
ENTRYPOINT ["dotnet", "JiApp.YtDownloader.dll"]
