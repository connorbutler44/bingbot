FROM mcr.microsoft.com/dotnet/runtime:6.0
COPY ./bin/Release/net6.0/publish .
ENTRYPOINT ["dotnet", "Bingbot.dll"]

RUN apt-get update && apt-get -y install libopus0 opus-tools libopus-dev libsodium-dev ffmpeg