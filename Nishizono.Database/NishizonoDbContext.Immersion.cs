using Microsoft.EntityFrameworkCore;
using Nishizono.Database.Models;

namespace Nishizono.Database;

public partial class NishizonoDbContext
{
    public async Task AddImmersionLog(ImmersionLog log)
    {
        ImmersionLogs.Add(log);
        await SaveChangesAsync();
    }
    public async Task RemoveImmersionLog(ImmersionLog log)
    {
        ImmersionLogs.Remove(log);
        await SaveChangesAsync();
    }
    public async Task AddProviderMetadataCache(ProviderMetadata cache)
    {
        ProviderMetadataCache.Add(cache);
        await SaveChangesAsync();
    }

    public async Task RemoveProviderMetadataCache(ProviderMetadata cache)
    {
        ProviderMetadataCache.Remove(cache);
        await SaveChangesAsync();
    }

    public async Task AddProviderQueryCache(ProviderQuery cache)
    {
        ProviderQueryCache.Add(cache);
        await SaveChangesAsync();
    }

    public async Task RemoveProviderQueryCache(ProviderQuery cache)
    {
        ProviderQueryCache.Remove(cache);
        await SaveChangesAsync();
    }

    public async Task<ProviderQuery> GetProviderQueryCache(string query, string providerName, MetadataType type)
    {
        return ProviderQueryCache.Where(_ => _.Query == query && _.Provider == providerName && _.Type == type).First();
    }

    public async Task<List<ProviderMetadata>> GetProviderMetadataCache(int query)
    {
        return ProviderMetadataCache.Where(_ => _.QueryId == query).ToList();
    }

    public async Task<IQueryable<ImmersionLog>> GetImmersionLogs(ulong userId)
    {
        return ImmersionLogs.Where(_ => _.UserId == userId);
    }

    public async Task<IQueryable<ImmersionLog>> GetImmersionLogs(ulong userId, DateTime since)
    {
        return ImmersionLogs.Where(_ => _.UserId == userId).Where(_ => _.TimeStamp > since);
    }
}
