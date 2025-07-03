using Bingbot;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace InteractionFramework
{
    public class InteractionHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _handler;
        private readonly IServiceProvider _services;
        private readonly IConfiguration _configuration;
        private readonly MessageHandler _messageHandler;

        public InteractionHandler(DiscordSocketClient client, InteractionService handler, IServiceProvider services, IConfiguration config, MessageHandler messageHandler)
        {
            _client = client;
            _handler = handler;
            _services = services;
            _configuration = config;
            _messageHandler = messageHandler;
        }

        public void InitializeAsync()
        {
            // Process when the client is ready, so we can register our commands.
            _client.Ready += async () =>
            {
                await _handler.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
                await _handler.RegisterCommandsGloballyAsync(true);
            };
            _client.InteractionCreated += async interaction =>
            {
                var ctx = new SocketInteractionContext(_client, interaction);
                var test = await _handler.ExecuteCommandAsync(ctx, _services);
            };
            _handler.Log += LogAsync;

            // Process all incoming messages for various use-cases
            _client.MessageReceived += _messageHandler.ProcessMessage;

            // Process when a reaction is added to a message
            _client.ReactionAdded += OnReactionAddedAsync;
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log);
            return Task.CompletedTask;
        }

        private async Task OnReactionAddedAsync(
            Cacheable<IUserMessage, ulong> cacheableMessage,
            Cacheable<IMessageChannel, ulong> cacheableChannel,
            SocketReaction reaction)
        {
            var message = await cacheableMessage.GetOrDownloadAsync();

            var user = reaction.User.Value ?? await _client.GetUserAsync(reaction.UserId);

            if (user.IsBot) return;

            return;
        }
    }
}
