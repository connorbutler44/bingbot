using System;

public class Poke
{
    public Guid Id { get; set; }
    public ulong SenderId { get; set; }
    public ulong RecipientId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}