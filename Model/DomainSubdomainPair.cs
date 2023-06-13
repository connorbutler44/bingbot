class DomainSubdomainPair
{
    public string Domain { get; set; }
    public string Subdomain { get; set; }

    public DomainSubdomainPair(string subdomain, string domain)
    {
        Domain = domain;
        Subdomain = subdomain;
    }
}
