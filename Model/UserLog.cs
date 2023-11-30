using System;

public class UserLog
{
    public Guid Id { get; set; }
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }
    public DateTime TakenAt { get; set; }
}