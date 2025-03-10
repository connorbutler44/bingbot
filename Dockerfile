FROM mcr.microsoft.com/dotnet/runtime:6.0-bookworm-slim-arm64v8

WORKDIR /app

COPY ./bin/Release/net6.0/publish .

RUN apt-get update && apt-get -y install libopus0 opus-tools libopus-dev libsodium-dev ffmpeg

ENTRYPOINT ["dotnet", "Bingbot.dll"]