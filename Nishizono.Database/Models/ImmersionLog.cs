namespace Nishizono.Database.Models;

/// <summary>
/// An Immersion Log entry stored in the bot's database.
/// </summary>
public class ImmersionLog
{
    /// <summary>
    /// The unique ID of this immersion log entry.
    /// </summary>
    public required int Id { get; set; }
    /// <summary>
    /// The user that created this immersion log entry.
    /// </summary>
    public required ulong UserId { get; set; }
    /// <summary>
    /// The guild this immersion log entry was created in.
    /// </summary>
    public required ulong GuildId { get; set; }
    /// <summary>
    /// The type of media logged in this immersion log entry.
    /// </summary>
    public required MediaType MediaType { get; set; }
    /// <summary>
    /// The amount of media consumed in this immersion log entry.
    /// </summary>
    public required int Amount { get; set; }
    /// <summary>
    /// Whether or not the Amount in this entry was interpolated using existing data in the database.
    /// </summary>
    public required bool Interpolated { get; set; }
    /// <summary>
    /// The duration of media consumption in this immersion log entry.
    /// </summary>
    public required TimeSpan Duration { get; set; }
    /// <summary>
    /// The creation timestamp of this immersion log entry.
    /// </summary>
    public required DateTime TimeStamp { get; set; }
    /// <summary>
    /// The plaintext, friendly title of this content or the user provided title for the content.
    /// </summary>
    public required string Title { get; set; }
    /// <summary>
    /// An identifier for the content being logged in this immersion log entry. (provider ID, etc.)
    /// </summary>
    public required string Content { get; set; }
    /// <summary>
    /// A comment note added by the user to describe the immersion log.
    /// </summary>
    public required string Comment { get; set; }
}

public enum MediaType
{
    VisualNovel,
    Manga,
    Anime,
    Book,
    Listening,
    Youtube,
    Anki
}