using System;
using System.IO;
using System.Threading.Tasks;
using Discord;

public class InstagramMessageProcessor : IMessageProcessor
{
    public async Task ProcessAsync(MessageContext messageContext)
    {
        Uri uri = messageContext.TryGetUrlForHost("instagram.com");

        if (uri is null)
            return;

        if (!uri.AbsolutePath.Contains("reel"))
            return;

        await using var mediaFile = await YtDlpService.DownloadStreamAsync(uri);
        await using FileStream stream = File.OpenRead(mediaFile.Path);

        // respond to original message with new video embed
        await messageContext.Message.Channel.SendFileAsync(stream, "media.mp4",
            messageReference: new MessageReference(messageContext.Message.Id));

        // remove original embed
        await messageContext.Message.ModifyAsync(m => m.Flags = MessageFlags.SuppressEmbeds);
    }
}