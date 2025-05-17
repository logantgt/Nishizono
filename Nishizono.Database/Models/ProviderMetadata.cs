using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nishizono.Database.Models;
public class ProviderMetadata
{
    /// <summary>
    /// Unique ID of the entry in the database.
    /// </summary>
    public required int Id { get; set; }
    /// <summary>
    /// The query that retrieved this metadata from the provider.
    /// </summary>
    public required int QueryId { get; set; }
    /// <summary>
    /// The title of the entry in plain English text.
    /// </summary>
    public required string Title { get; set; }
    /// <summary>
    /// The title of the entry in the original language text.
    /// </summary>
    public required string NativeTitle { get; set; }
    /// <summary>
    /// ID used by the Provider to reference the media.
    /// </summary>
    public required string ProviderId { get; set; }
    /// <summary>
    /// The Provider that retrived this data.
    /// </summary>
    public required string Url { get; set; }
    /// <summary>
    /// A URL to the cover image representing this media.
    /// </summary>
    public required string Image { get; set; }
}

public enum MetadataType
{
    VisualNovel,
    Anime,
    Manga,
    Youtube
}