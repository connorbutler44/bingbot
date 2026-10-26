using System.Threading.Tasks;
using Discord.Interactions;
using System;
using Discord;

namespace Bingbot.Modules
{
    public class ChatModule : InteractionModuleBase<SocketInteractionContext>
    {
        IServiceProvider _provider;

        public ChatModule(IServiceProvider provider)
        {
            _provider = provider;
        }

        [SlashCommand("send-message", "Send a message as Bingbot", runMode: RunMode.Async)]
        public async Task SendMessage(
            [Summary(description: "Message to send")]
            string message)
        {
            await DeferAsync(ephemeral: true);

            await Context.Channel.SendMessageAsync(message);

            await DeleteOriginalResponseAsync();
        }

        [MessageCommand("Reply as Bingbot")]
        public async Task ReplyAs(IMessage target)
        {
            await RespondWithModalAsync<ReplyModal>($"reply-as:{target.Id}");
        }

        [ModalInteraction("reply-as:*", ignoreGroupNames: true)]
        public async Task ReplyAsSubmitted(string targetId, ReplyModal modal)
        {
            await DeferAsync(ephemeral: true);

            await Context.Channel.SendMessageAsync(
                modal.Message,
                messageReference: new MessageReference(ulong.Parse(targetId), Context.Channel.Id, Context.Guild?.Id));

            await DeleteOriginalResponseAsync();
        }
    }

    public class ReplyModal : IModal
    {
        public string Title => "Reply as Bingbot";

        [InputLabel("Message")]
        [ModalTextInput("message", TextInputStyle.Paragraph)]
        public string Message { get; set; } = string.Empty;
    }
}