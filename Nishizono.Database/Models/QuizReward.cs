namespace Nishizono.Database.Models;

/// <summary>
/// A Quiz Reward entry stored in the bot's database.
/// </summary>
public class QuizReward
{
    /// <summary>
    /// The Snowflake derived ulong ID of the role attached to this reward.
    /// </summary>
    public required ulong Id { get; set; }
    /// <summary>
    /// The weight used to determine the sorting of the entry when listing quiz rewards.
    /// </summary>
    public required int Sort { get; set; }
    /// <summary>
    /// The string that initiates the quiz intended to give this reward.
    /// </summary>
    public required string Command { get; set; }
    /// <summary>
    /// The friendly name of the reward.
    /// </summary>
    public required string Name { get; set; }
    /// <summary>
    /// The cooldown period between uses of the quiz used for this reward.
    /// </summary>
    public required TimeSpan Cooldown { get; set; }
}