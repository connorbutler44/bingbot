using System.Linq;
using System.Threading.Tasks;
using Discord;

public class JarradMessageProcessor : IMessageProcessor
{
    public async Task ProcessAsync(MessageContext messageContext)
    {
        if (messageContext.Message.Author.Id == 905417457232142366 &&
            messageContext.Message.Channel.Id == 310249048844271628 &&
            messageContext.Message.Author.ActiveClients.Any(p => p.ToString() == "Mobile"))
        {
            var emote = Emote.Parse("<:ICANT:1143297524396990595>");
            await messageContext.Message.AddReactionAsync(emote);
        }
    }
}