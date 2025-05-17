using Nishizono.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nishizono.Providers
{
    public interface IMetadataProvider
    {
        /// <summary>
        /// Query the provider for results. On failure, returns an empty list of MediaMetadata.
        /// </summary>
        /// <param name="query">The string to query the provider with. This can be a media title, or an ID used by the provider to reference the media.</param>
        /// <returns></returns>
        public Task<List<ProviderMetadata>> QueryAsync(MetadataType metadataType, QueryType queryType, string query, bool cache = true);
        /// <summary>
        /// The name of the provider.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// The URL of the provider.
        /// </summary>
        public string Url { get; }
    }
}
