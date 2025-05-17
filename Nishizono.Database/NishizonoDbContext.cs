using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Nishizono.Database.Models;

namespace Nishizono.Database;

public partial class NishizonoDbContext : DbContext
{
    private ILogger<DbContext> _logger;
    public DbSet<GuildConfig> GuildConfigs { get; set; }
    public DbSet<UserConfig> UserConfigs { get; set; }
    public DbSet<ImmersionLog> ImmersionLogs { get; set; }
    public DbSet<QuizReward> QuizRewards { get; set; }
    public DbSet<ProviderMetadata> ProviderMetadataCache { get; set; }
    public DbSet<ProviderQuery> ProviderQueryCache { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql(Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING"));

    public NishizonoDbContext(ILogger<DbContext> logger)
    {
        _logger = logger;
    }
}
