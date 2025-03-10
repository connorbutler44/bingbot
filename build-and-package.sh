#!/bin/bash

# restore dependencies
dotnet restore

# build project
dotnet publish --configuration Release

# create docker image
docker build -t bingbot:latest .

mkdir -p ./build

# save docker image
docker save -o ./build/bingbot.tar bingbot:latest
gzip ./build/bingbot.tar