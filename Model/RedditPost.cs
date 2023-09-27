class RedditPost
{
    public string Kind { get; set; }
    public RedditPostData Data { get; set; }
}

class RedditPostData
{
    public RedditPostDataChildren[] Children { get; set; }
}

class RedditPostDataChildren
{
    public RedditPostDataChildrenData Data { get; set; }
}

class RedditPostDataChildrenData
{
    public string Post_Hint { get; set; }
    public RedditPostMedia Media { get; set; }
}

class RedditPostMedia
{
    public RedditPostMediaVideo Reddit_Video { get; set; }
}

class RedditPostMediaVideo
{
    public string Fallback_Url { get; set; }
    public string Hls_Url { get; set; }
}
