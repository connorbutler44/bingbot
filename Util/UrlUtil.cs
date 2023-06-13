using System;
using System.Collections.Generic;

static class UrlUtil
{
    public static Boolean UrlMatchesDomainAndSubdomain(string url, List<DomainSubdomainPair> domainSubdomains)
    {
        // parse url
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        // check that the url matches one of the specified domain + subdomains
        foreach (var pair in domainSubdomains)
        {
            if (uri.Host.Equals($"{pair.Subdomain}.{pair.Domain}", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static Boolean UrlMatchesDomainAndSubdomain(string url, DomainSubdomainPair domainSubdomain)
    {
        // parse url
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        // check that the url matche the specified domain + subdomain
        if (uri.Host.Equals($"{domainSubdomain.Subdomain}.{domainSubdomain.Domain}", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}