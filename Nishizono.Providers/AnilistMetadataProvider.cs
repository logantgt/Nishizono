using GraphQL.Client.Abstractions;
using GraphQL;
using GraphQL.Client.Http;
using Nishizono.Database;
using Nishizono.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GraphQL.SystemTextJson;
using GraphQL.Client.Serializer.SystemTextJson;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using GraphQL.Validation;

namespace Nishizono.Providers
{
    public class AnilistMetadataProvider : CacheableProvider, IMetadataProvider
    {
        private GraphQLHttpClient _client;
        public AnilistMetadataProvider(NishizonoDbContext database) : base(database)
        {
            _client = new GraphQLHttpClient("https://graphql.anilist.co", new SystemTextJsonSerializer());
        }

        public string Name => "Anilist";

        public string Url => "https://anilist.co";

        public async Task<List<ProviderMetadata>> QueryAsync(MetadataType metadataType, QueryType queryType, string query, bool cache = true)
        {
            List<ProviderMetadata> metadata = new();
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2) return metadata;

            if (cache is true) metadata = await base.Retrieve(query, this.Name, metadataType);
            if (metadata.Count > 0) return metadata;

            var AnilistRequest = new GraphQLRequest
            {
                Query = @"query ($id: Int, $page: Int, $perPage: Int, $search: String, $type: MediaType) {
                  Page (page: $page, perPage: $perPage) {
                    pageInfo {
                      currentPage
                      hasNextPage
                      perPage
                    }
                    media (id: $id, search: $search, type: $type) {
                      id
                      siteUrl,
                      title {
                        romaji
                        native
                      }
                      coverImage {
                        extraLarge
                      }
                    }
                  }
                }",
            };

            if (queryType == QueryType.Title)
            {
                AnilistRequest.Variables = new
                {
                    search = query,
                    page = 1,
                    perPage = 6,
                    type = metadataType
                };
            }
            else if (queryType == QueryType.Id)
            {
                AnilistRequest.Variables = new
                {
                    id = query,
                    page = 1,
                    perPage = 1,
                    type = metadataType
                };
            }

            GraphQLResponse<AnilistResponse> response = await _client.SendQueryAsync<AnilistResponse>(AnilistRequest);

            if(response.Errors is not null) return new List<ProviderMetadata>();

            foreach (AnilistResultMedium result in response.Data.Page.Media)
            {
                if (result.Title is null || result.Id is null || result.SiteUrl is null || result.CoverImage is null) continue;
                metadata.Add(new ProviderMetadata
                {
                    Id = 0,
                    QueryId = 0,
                    Title = result.Title.Romaji,
                    NativeTitle = result.Title.Native,
                    ProviderId = $"{result.Id}",
                    Url = result.SiteUrl,
                    Image = result.CoverImage.ExtraLarge
                });
            }

            if (cache is true) await base.Cache(query, this.Name, metadataType, metadata);

            return metadata;
        }
    }

    public class AnilistResponse
    {
        [JsonPropertyName("page")]
        public AnilistResultPage? Page { get; set; }
    }

    public class AnilistResultPage
    {
        [JsonPropertyName("pageInfo")]
        public AnilistResultPageInfo? PageInfo { get; set; }
        [JsonPropertyName("media")]
        public AnilistResultMedium[]? Media { get; set; }
    }

    public class AnilistResultPageInfo
    {
        [JsonPropertyName("currentPage")]
        public int? CurrentPage { get; set; }
        [JsonPropertyName("hasNextPage")]
        public bool? HasNextPage { get; set; }
        [JsonPropertyName("perPage")]
        public int? PerPage { get; set; }
    }

    public class AnilistResultMedium
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }
        [JsonPropertyName("title")]
        public AnilistResultTitle? Title { get; set; }
        [JsonPropertyName("siteUrl")]
        public string? SiteUrl {  get; set; }
        [JsonPropertyName("coverImage")]
        public AnilistResultCoverImage? CoverImage { get; set; }
    }

    public class AnilistResultTitle
    {
        [JsonPropertyName("romaji")]
        public string? Romaji { get; set; }
        [JsonPropertyName("native")]
        public string? Native { get; set; }
    }

    public class AnilistResultCoverImage
    {
        [JsonPropertyName("extraLarge")]
        public string? ExtraLarge { get; set; }
    }
}
