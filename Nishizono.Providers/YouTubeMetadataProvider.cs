using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Nishizono.Database;
using Nishizono.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nishizono.Providers
{
    public class YouTubeMetadataProvider : CacheableProvider, IMetadataProvider
    {
        public YouTubeMetadataProvider(NishizonoDbContext database) : base(database)
        {
        }

        public string Name => "YouTube";

        public string Url => "https://youtube.com";

        public async Task<List<ProviderMetadata>> QueryAsync(MetadataType metadataType, QueryType queryType, string query, bool cache = true)
        {
            List<ProviderMetadata> metadata = new();
            if (queryType != QueryType.Id) return metadata;
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2) return metadata;

            if (cache is true) metadata = await base.Retrieve(query, this.Name, metadataType);
            if (metadata.Count > 0) return metadata;

            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = Environment.GetEnvironmentVariable("YOUTUBE_DATA_V3_API_KEY"),
                ApplicationName = this.GetType().ToString()
            });

            var v = youtubeService.Videos.List("snippet");

            string ytLink = query;

            if (Uri.TryCreate(query, UriKind.Absolute, out var update))
            {
                ytLink = update.AbsolutePath.Substring(1);
            }
            v.Id = ytLink;
            v.MaxResults = 1;

            var vo = await v.ExecuteAsync();

            if (vo.Items.Count > 0)
            {
                metadata.Add(new ProviderMetadata
                {
                    Id = 0,
                    QueryId = 0,
                    Title = vo.Items.First().Snippet.Title,
                    NativeTitle = "",
                    ProviderId = vo.Items.First().Id,
                    Url = $"https://youtu.be/{vo.Items.First().Id}",
                    Image = vo.Items.First().Snippet.Thumbnails.High.Url
                });
            }
            else
            {
                metadata.Add(new ProviderMetadata
                {
                    Id = 0,
                    QueryId = 0,
                    Title = $"Livestream",
                    NativeTitle = "",
                    ProviderId = ytLink,
                    Url = $"{this.Url}/{ytLink}",
                    Image = ""
                });
            }

            if (cache is true) await base.Cache(query, this.Name, metadataType, metadata);

            return metadata;
        }
    }
}
