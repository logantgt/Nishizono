using Nishizono.Database;
using Nishizono.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nishizono.Providers;
public class CacheableProvider
{
    private readonly NishizonoDbContext _database;
    public CacheableProvider(NishizonoDbContext database)
    {
        _database = database;
    }
    public async Task Cache(string query, string providerName, MetadataType metadataType, List<ProviderMetadata> results)
    {
        await _database.AddProviderQueryCache(new ProviderQuery()
        {
            Id = 0,
            Query = query,
            Provider = providerName,
            Type = metadataType,
            InvalidAt = DateTime.UtcNow.AddMonths(2)
        });

        foreach (var item in results)
        {
            await _database.AddProviderMetadataCache(new ProviderMetadata()
            {
                Id = 0,
                QueryId = (await _database.GetProviderQueryCache(query, providerName, metadataType)).Id, // TODO: figure out how to reference the query that was just cached rather than finding it again
                Title = item.Title,
                NativeTitle = item.NativeTitle,
                ProviderId = item.ProviderId,
                Url = item.Url,
                Image = item.Image
            });
        }
    }
    public async Task<List<ProviderMetadata>> Retrieve(string query, string providerName, MetadataType metadataType)
    {
        // TODO: rewrite this function, it sucks
        List<ProviderMetadata> results = new List<ProviderMetadata>();
        ProviderQuery cached;
        try { cached = await _database.GetProviderQueryCache(query, providerName, metadataType); } catch { return results; }
        results = await _database.GetProviderMetadataCache(cached.Id);

        if (cached.InvalidAt.CompareTo(DateTime.UtcNow) <= 0)
        {
            // cache is invalid, delete it and every metadata entry that used it
            foreach (var item in results)
            {
                await _database.RemoveProviderMetadataCache(item);
            }

            await _database.RemoveProviderQueryCache(cached);
            results = new List<ProviderMetadata>();
        }

        return results;
    }
}
