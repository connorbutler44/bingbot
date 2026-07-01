using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Discord.WebSocket;

public class MessageContext
{
    public SocketUserMessage Message { get; }

    public IReadOnlyList<Uri> MessageUrls { get; }

    public MessageContext(SocketUserMessage message)
    {
        Message = message;

        MatchCollection urlMatches = Regex.Matches(message.Content, @"\b(?:https?:\/\/|www\.)\S+\b");

        List<Uri> urls = urlMatches
            .Select(m => Uri.TryCreate(m.Value, UriKind.Absolute, out var url) ? url : null)
            .Where(url => url is not null)
            .ToList();

        MessageUrls = urls;
    }

    public Uri TryGetUrlForHost(string host)
    {
        return MessageUrls.FirstOrDefault(x => x.Host.Replace("www.", "") == host);
    }
}