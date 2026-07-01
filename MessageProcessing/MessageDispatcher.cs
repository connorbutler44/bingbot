using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;

public class MessageDispatcher
{
    private readonly IEnumerable<IMessageProcessor> _processors;

    public MessageDispatcher(IEnumerable<IMessageProcessor> processors)
    {
        _processors = processors;
    }

    public async Task HandleMessageAsync(SocketMessage socketMessage)
    {
        if (socketMessage is not SocketUserMessage message)
        {
            return;
        }

        if (message.Author.IsBot)
        {
            return;
        }

        var context = new MessageContext(message);

        foreach (var processor in _processors)
        {
            await processor.ProcessAsync(context);
        }
    }
}