namespace Nishizono.Database.Models;

/// <summary>
/// A Guild Configuration entry represented in the bot's database.
/// </summary>
public class UserConfig
{
    /// <summary>
    /// The ID of the user represented in this configuration.
    /// </summary>
    public required ulong Id { get; set; }
    /// <summary>
    /// The quiz cooldowns currently applied to the user.
    /// The key is a Discord Role Snowflake attached to the quiz that is on cooldown,
    /// and the value is the date and time (Utc) when the cooldown expires.
    /// </summary>
    public required Dictionary<string, string> QuizCooldowns { get; set; }
}