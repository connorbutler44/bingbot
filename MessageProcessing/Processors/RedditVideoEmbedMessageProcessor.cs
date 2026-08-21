using System;
using System.Threading.Tasks;
using System.IO;
using Discord;


public class RedditMessageProcessor : IMessageProcessor
{
    public async Task ProcessAsync(MessageContext messageContext)
    {
        Uri? uri = messageContext.TryGetUrlForHost("reddit.com");

        if (uri is null)
        {
            return;
        }

        await using var mediaFile = await YtDlpService.DownloadStreamAsync(uri);
        await using FileStream stream = File.OpenRead(mediaFile.Path);

        // respond to original message with new video embed
        await messageContext.Message.Channel.SendFileAsync(stream, "media.mp4",
            messageReference: new MessageReference(messageContext.Message.Id), isSpoiler: messageContext.MessageHasSpoiler);

        // make sure temp file is disposed of
        await mediaFile.DisposeAsync();

        // remove original embed
        await messageContext.Message.ModifyAsync(m => m.Flags = MessageFlags.SuppressEmbeds);
    }
}