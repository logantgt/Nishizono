using Nishizono.Database;
using Nishizono.Database.Models;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nishizono.Providers;

public class VndbMetadataProvider : CacheableProvider, IMetadataProvider
{
    private HttpClient _client;

    public VndbMetadataProvider(HttpClient client, NishizonoDbContext database) : base(database)
    {
        _client = client;
    }

    public string Name => "vndb";

    public string Url => "https://vndb.org";

    public async Task<List<ProviderMetadata>> QueryAsync(MetadataType metadataType, QueryType queryType, string query, bool cache = true)
    {
        List<ProviderMetadata> metadata = new();
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2) return metadata;

        if (cache is true) metadata = await base.Retrieve(query, this.Name, metadataType);
        if (metadata.Count > 0) return metadata;

        VndbVnResponse response;
        
        if (queryType == QueryType.Id)
        {
            response = await VndbQueryAsync(["id", "=", query]);
        }
        else
        {
            response = await VndbQueryAsync(["search", "=", query]);
        }
        
        if (response is null || response.Results is null) return new List<ProviderMetadata>();

        foreach (VndbVnResult result in response.Results)
        {
            if (result.Title is null || result.Id is null || result.Image is null || result.Image.Url is null) continue;

            if (result.AltTitle is null) result.AltTitle = "";

            metadata.Add(new ProviderMetadata
            {
                Id = 0,
                QueryId = 0,
                Title = result.Title,
                NativeTitle = result.AltTitle,
                ProviderId = result.Id,
                Url = $"{this.Url}/{result.Id}",
                Image = result.Image.Url
            });
        }

        if (cache is true) await base.Cache(query, this.Name, metadataType, metadata);

        return metadata;
    }

    internal async Task<VndbVnResponse> VndbQueryAsync(string[] filters)
    {
        var content = JsonContent.Create(new VndbVnQuery
        {
            Filters = filters,
            Fields = "id, title, alttitle, image.url"
        });

        HttpResponseMessage? response = await _client.PostAsync("https://api.vndb.org/kana/vn", content);

        return await JsonSerializer.DeserializeAsync<VndbVnResponse>(response.Content.ReadAsStream());
    }
}

/// <summary>
/// A VN related query made to the vndb api.
/// </summary>
internal class VndbVnQuery
{
    /// <summary>
    /// Filters are used to determine which database items to fetch, see the section on Filters. 
    /// </summary>
    [JsonPropertyName("filters")]
    public string[]? Filters { get; set; }
    /// <summary>
    /// Comma-separated list of fields to fetch for each database item. Dot notation can be used to select nested
    /// JSON objects, e.g. "image.url" will select the url field inside the image object. Multiple nested fields
    /// can be selected with brackets, e.g. "image{id,url,dims}" is equivalent to "image.id, image.url, image.dims". 
    /// Every field of interest must be explicitely mentioned, there is no support for wildcard matching
    /// The same applies to nested objects, it is an error to list image without sub-fields in the example above.
    /// The top-level id field is always selected by default and does not have to be mentioned in this list.
    /// </summary>
    [JsonPropertyName("fields")]
    public string? Fields { get; set; }
    /// <summary>
    /// Field to sort on. Supported values depend on the type of data being queried and are documented separately. 
    /// </summary>
    [JsonPropertyName("sort")]
    public string? Sort { get; set; }
    /// <summary>
    /// Set to true to sort in descending order. 
    /// </summary>
    [JsonPropertyName("reverse")]
    public bool? Reverse { get; set; }
    /// <summary>
    /// Number of results per page, max 100. Can also be set to 0 if you’re not interested in the results at all,
    /// but just want to verify your query or get the count, compact_filters or normalized_filters. 
    /// </summary>
    [JsonPropertyName("results")]
    public int? Results { get; set; }
    /// <summary>
    /// Page number to request, starting from 1. See also the note on pagination below. 
    /// </summary>
    [JsonPropertyName("page")]
    public int? Page { get; set; }
    /// <summary>
    /// User ID. This field is mainly used for POST /ulist, but it also sets the default user ID to use for the
    /// visual novel “label” filter. Defaults to the currently authenticated user. 
    /// </summary>
    [JsonPropertyName("user")]
    public string? User { get; set; }
    /// <summary>
    /// Whether the response should include the count field. This option should be avoided when the count is not
    /// needed since it has a considerable performance impact. 
    /// </summary>
    [JsonPropertyName("count")]
    public bool? Count { get; set; }
    /// <summary>
    /// Whether the response should include the compact_filters field.
    /// </summary>
    [JsonPropertyName("compact_filters")]
    public bool? CompactFilters { get; set; }
    /// <summary>
    /// Whether the response should include the normalized_filters field
    /// </summary>
    [JsonPropertyName("normalized_filters")]
    public bool? NormalizedFilters { get; set; }
}

/// <summary>
/// A VN related response received from the vndb api.
/// </summary>
internal class VndbVnResponse
{
    /// <summary>
    /// Array of objects representing the query results. 
    /// </summary>
    [JsonPropertyName("results")]
    public VndbVnResult[]? Results { get; set; }
    /// <summary>
    /// When true, repeating the query with an incremented page number will yield more results.
    /// This is a cheaper form of pagination than using the count field. 
    /// </summary>
    [JsonPropertyName("more")]
    public bool? More { get; set; }
    /// <summary>
    /// Only present if the query contained "count":true. Indicates the total number of entries that matched the given filters. 
    /// </summary>
    [JsonPropertyName("count")]
    public int? Count { get; set; }
    /// <summary>
    /// Only present if the query contained "compact_filters":true. This is a compact string representation of the filters given in the query. 
    /// </summary>
    [JsonPropertyName("compact_filters")]
    public string? CompactFilters { get; set; }
    /// <summary>
    /// Only present if the query contained "normalized_filters":true. This is a normalized JSON representation of the filters given in the query. 
    /// </summary>
    [JsonPropertyName("normalized_filters")]
    public string? NormalizedFilters { get; set; }
}

/// <summary>
/// A partial VN result from a vndb api VN query.
/// This result only serializes the results of a query made with the "id, title, image.url" fields.
/// </summary>
internal class VndbVnResult
{
    /// <summary>
    /// The title of the VN in plain English text.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }
    /// <summary>
    /// The title of the VN in the original script.
    /// </summary>
    [JsonPropertyName("alttitle")]
    public string? AltTitle { get; set; }
    /// <summary>
    /// The vndb id of the VN.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    /// <summary>
    /// The image of the VN.
    /// </summary>
    [JsonPropertyName("image")]
    public VndbVnResultImage? Image { get; set; }
}

/// <summary>
/// An image from a vndb API vn query result.
/// </summary>
internal class VndbVnResultImage
{
    /// <summary>
    /// The URL of the VN image.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}
