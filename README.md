# bingbot
Fun little Discord bot using .NET to make cool/fun stuff for my friends 😸

## Running Locally
For the bot to connect to a voice channel and send audio *in local development*, you will need the following (note that this isn't necessary for a deployed env using Docker)
  * `libsodium` and `opus` - https://github.com/discord-net/Discord.Net/blob/dev/voice-natives/vnext_natives_win32_x64.zip
    * Extract `libsodium.dll` and `libopus.dll` from the above download
    * Rename `libopus.dll` to `opus.dll`
    * Move both to where the application is being run from (ex. `./bin/Debug/net6.0`)
  * `ffmpeg` - https://ffmpeg.org/download.html

## DiscordNET
This project uses DiscordNET's [interaction framework](https://discordnet.dev/guides/int_framework/intro.html) for handling commands

Commands are organized into modules which are found in the `Modules` directory. Within a module you can create commands and sub-commands

The interaction framework handles automatically creating/updating/deleting commands on the server(s) when the bot starts

## Environment Variables
| Key                 | Description                                      |
| ------------------- | ------------------------------------------------ |
| DISCORD_API_KEY     | API key for your discord application (if you don't already have a bot application, follow at least step 1 [https://discord.com/developers/docs/getting-started](here))            |
| ELEVEN_LABS_API_KEY | API key for the ElevenLabs Text-to-Speech API    |
| OPEN_API_KEY        | OpenAPI key for GPT & Dall-E 3                   |

## Bot Permissions
If you don't want to give the bot administrator permission, you will need to use (at least) the following permission set for the bot to be able to use the full suite of features

![image](https://github.com/connorbutler44/bingbot/assets/15933735/ec23c038-ba6e-42c0-ae83-de55b9847b7d)


![image](https://user-images.githubusercontent.com/15933735/170916400-606ed630-6f2c-4ba0-9f2a-9088d107809f.png)
