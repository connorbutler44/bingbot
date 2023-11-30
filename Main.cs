using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Discord.Interactions;
using InteractionFramework;

namespace Bingbot
{
    class Program
    {

        private readonly IServiceProvider _services;
        private readonly IConfiguration _configuration;
        private readonly DiscordSocketConfig _socketConfig = new()
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers | GatewayIntents.MessageContent | GatewayIntents.GuildPresences,
            AlwaysDownloadUsers = true,
        };

        static void Main(string[] args)
            => new Program()
                .RunAsync()
                .GetAwaiter()
                .GetResult();

        public Program()
        {
            _configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables("BINGBOT_")
                .Build();

            _services = new ServiceCollection()
                .AddSingleton(_configuration)
                .AddSingleton(_socketConfig)
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                .AddSingleton<InteractionHandler>()
                .AddSingleton<AudioChannelManager>()
                .AddSingleton<MessageHandler>()
                .AddSingleton<ChatService>()
                .AddSingleton<ElevenLabsTextToSpeechService>()
                .AddDbContext<DataContext>()
                .BuildServiceProvider();
        }

        public async Task RunAsync()
        {
            var client = _services.GetRequiredService<DiscordSocketClient>();

            client.Log += LogAsync;

            await _services.GetRequiredService<InteractionHandler>()
                .InitializeAsync();

            string apiKey = _configuration["DISCORD_API_KEY"];

            await client.LoginAsync(TokenType.Bot, apiKey);
            await client.StartAsync();

            // Block the program until it is closed.
            await Task.Delay(Timeout.Infinite);
        }

        private Task LogAsync(LogMessage log)
        {
            return Task.CompletedTask;
        }
    }
}
