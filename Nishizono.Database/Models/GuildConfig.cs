namespace Nishizono.Database.Models;

/// <summary>
/// A Guild Configuration entry represented in the bot's database.
/// </summary>
public class GuildConfig
{
    /// <summary>
    /// The ID of the guild represented in this configuration.
    /// </summary>
    public required ulong Id { get; set; }
    /// <summary>
    /// Whether or not Immersion Logging functionality should be enabled in the guild.
    /// </summary>
    public required bool ImmersionLoggingEnabled { get; set; }
    /// <summary>
    /// Whether or not Immersion Logging activity from this guild should be publically accessible.
    /// </summary>
    public required bool ImmersionLoggingPublic { get; set; }
    /// <summary>
    /// The Snowflake derived ulong ID of the channel that may be used for immersion logging messages in the guild.
    /// </summary>
    public required ulong ImmersionLoggingChannel { get; set; }
    /// <summary>
    /// The Snowflake derived ulong IDs of the channels that may be watched for quiz activity in the guild.
    /// </summary>
    public required List<ulong> QuizChannels { get; set; }

    /// <summary>
    /// The Snowflake derived ulong IDs of the roles that may be given for completing quizzes in the guild.
    /// </summary>
    public required List<ulong> QuizRewards { get; set; }
    /// <summary>
    /// The Snowflake derived ulong ID of the channel that may be used for posting public notifications of user achievement.
    /// </summary>
    public required ulong NotificationChannel { get; set; }
}