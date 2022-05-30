public class TtsResponse
{
    public TtsData data { get; set; }
}

public class TtsData
{
    public string v_str { get; set; }
    public string s_key { get; set; }
    public string message { get; set; }
    public int status_code { get; set; }
    public string status_msg { get; set; }
}