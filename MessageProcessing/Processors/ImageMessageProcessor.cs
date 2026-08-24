using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Discord.WebSocket;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

public class ImageMessageProcessor : IMessageProcessor
{
    public async Task ProcessAsync(MessageContext messageContext)
    {
        foreach (var attachment in messageContext.Message.Attachments)
        {
            if (attachment.ContentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true)
                await SendImageWithWrapperAsync(messageContext, attachment);

            using var httpClient = new HttpClient();
        }
    }

    public async Task SendImageWithWrapperAsync(
        MessageContext messageContext,
        Discord.IAttachment attachment)
    {
        using var httpClient = new HttpClient();

        // get original image
        await using var imageStream = await httpClient.GetStreamAsync(attachment.Url);

        using var image = await Image.LoadAsync<Rgba32>(imageStream);

        // canvas layout consts
        const int horizontalPadding = 25;
        const int topPadding = 75;
        const int bottomPadding = 25;
        const int insideCornerRadius = 12;
        const int outsideCornerRadius = 20;

        var outputWidth = image.Width + horizontalPadding * 2;
        var outputHeight = image.Height + topPadding + bottomPadding;

        using var output = new Image<Rgba32>(
            Configuration.Default,
            outputWidth,
            outputHeight,
            new Rgba32(13, 17, 20, 255));

        // round original image corners
        RoundCorners(image, insideCornerRadius);

        // create icon for server
        var guildChannel = messageContext.Message.Channel as SocketGuildChannel;
        var guild = guildChannel?.Guild;

        var communityName = guild?.Name ?? "Discord";
        var iconUrl = guild?.IconUrl;

        await using var iconStream =
            await httpClient.GetStreamAsync(iconUrl);

        using var icon =
            await Image.LoadAsync<Rgba32>(iconStream);

        const int iconSize = 30;

        icon.Mutate(x => x.Resize(iconSize, iconSize));
        MakeCircle(icon);

        // prepare text
        var regularFont = SystemFonts.CreateFont("Arial", 24);
        var boldFont = SystemFonts.CreateFont("Arial", 24, FontStyle.Bold);

        var prefix = $"From {communityName} community on";
        var discordText = " Discord";

        var text = prefix + discordText;

        var textOptions = new RichTextOptions(regularFont)
        {
            Origin = new PointF(
                horizontalPadding + iconSize + 8,
                (topPadding - regularFont.Size) / 2),

            TextRuns = new[]
            {
                new RichTextRun
                {
                    Start = 5,
                    End = 5 + communityName.Length,
                    Font = boldFont,
                    Brush = Brushes.Solid(Color.White)
                },

                new RichTextRun
                {
                    Start = prefix.Length,
                    End = text.Length,
                    Font = boldFont,
                    Brush = Brushes.Solid(Color.Parse("#5865F2"))
                }
            }
        };

        // get watermark
        const string watermarkUrl = "https://cdn.discordapp.com/icons/980553775121051789/989fa1248a3cc717b09ce44d7527588a.png?size=128";

        await using var watermarkStream =
            await httpClient.GetStreamAsync(watermarkUrl);

        using var watermark =
            await Image.LoadAsync<Rgba32>(watermarkStream);

        const float watermarkScale = 0.10f;
        const float insetPercent = 0.05f;
        const float watermarkOpacity = 0.5f;

        var watermarkWidth = (int)(image.Width * watermarkScale);

        var watermarkHeight =
            (int)((float)watermark.Height / watermark.Width * watermarkWidth);

        watermark.Mutate(x =>
        {
            x.Resize(watermarkWidth, watermarkHeight);
            x.Opacity(watermarkOpacity);
        });

        MakeCircle(watermark);

        var inset = (int)(image.Width * insetPercent);

        var watermarkX =
            horizontalPadding +
            image.Width -
            watermarkWidth -
            inset;

        var watermarkY =
            topPadding +
            image.Height -
            watermarkHeight -
            inset;

        // compose layers to new output
        output.Mutate(ctx =>
        {
            // original image
            ctx.DrawImage(
                image,
                new Point(horizontalPadding, topPadding),
                1f);

            if (icon != null)
            {
                ctx.DrawImage(
                    icon,
                    new Point(horizontalPadding, (topPadding - iconSize) / 2),
                    1f);
            }

            ctx.DrawText(
                textOptions,
                text,
                Brushes.Solid(Color.LightGray),
                pen: null);

            ctx.DrawImage(
                watermark,
                new Point(watermarkX, watermarkY),
                1f);
        });

        // round final image corners
        RoundCorners(output, outsideCornerRadius);

        using var outputStream = new MemoryStream();
        await output.SaveAsPngAsync(outputStream);
        outputStream.Position = 0;

        await messageContext.Message.Channel.SendFileAsync(outputStream,
            attachment.Filename,
            messageReference: new Discord.MessageReference(messageContext.Message.Id),
            isSpoiler: messageContext.MessageHasSpoiler);
    }

    private static void MakeCircle(Image<Rgba32> image)
    {
        RoundCorners(image, image.Width / 2);
    }

    private static void RoundCorners(
        Image<Rgba32> image,
        int radius)
    {
        var width = image.Width;
        var height = image.Height;

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < height; y++)
            {
                var row = accessor.GetRowSpan(y);

                for (var x = 0; x < width; x++)
                {
                    var outside =
                        (x < radius &&
                         y < radius &&
                         Distance(
                             x,
                             y,
                             radius,
                             radius) > radius)

                        ||

                        (x >= width - radius &&
                         y < radius &&
                         Distance(
                             x,
                             y,
                             width - radius - 1,
                             radius) > radius)

                        ||

                        (x < radius &&
                         y >= height - radius &&
                         Distance(
                             x,
                             y,
                             radius,
                             height - radius - 1) > radius)

                        ||

                        (x >= width - radius &&
                         y >= height - radius &&
                         Distance(
                             x,
                             y,
                             width - radius - 1,
                             height - radius - 1) > radius);

                    if (outside)
                    {
                        row[x].A = 0;
                    }
                }
            }
        });
    }

    private static double Distance(
        int x1,
        int y1,
        int x2,
        int y2)
    {
        var dx = x1 - x2;
        var dy = y1 - y2;

        return Math.Sqrt(dx * dx + dy * dy);
    }
}