using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

public class DataContext : DbContext
{
    IConfiguration _config;

    public DataContext(DbContextOptions<DataContext> options, IConfiguration config) : base(options)
    {
        _config = config;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        Console.WriteLine(_config["POSTGRES_CONNECTION_STRING"], _config);
        optionsBuilder.UseNpgsql(_config["POSTGRES_CONNECTION_STRING"])
            .UseSnakeCaseNamingConvention();
    }

    public DbSet<UserLog> UserLogs { get; set; }
    public DbSet<Poke> Pokes { get; set; }
    public DbSet<UserSetting> UserSettings { get; set; }
}