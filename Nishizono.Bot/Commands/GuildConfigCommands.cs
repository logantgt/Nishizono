using System.ComponentModel;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Rest.Core;
using Remora.Results;
using Nishizono.Database;
using Nishizono.Database.Models;
using System.Text;
using Remora.Discord.Commands.Extensions;

/// <summary>
/// Responds to Guild Config commands.
/// </summary>
namespace Nishizono.Bot.Commands;
public class GuildConfigCommands : CommandGroup
{
    private readonly FeedbackService _feedbackService;
    private readonly IInteractionCommandContext _context;
    private readonly NishizonoDbContext _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="GuildConfigCommands"/> class.
    /// </summary>
    /// <param name="feedbackService">The feedback service.</param>
    public GuildConfigCommands(FeedbackService feedbackService, IInteractionCommandContext interactionContext, NishizonoDbContext database)
    {
        _feedbackService = feedbackService;
        _context = interactionContext;
        _database = database;
    }

    [Command("add-quiz-reward")]
    [Description("Add a reward for completing a Kotoba quiz.")]
    [DiscordDefaultMemberPermissions(DiscordPermission.Administrator)]
    [Ephemeral]
    public async Task<IResult> RecordGuildQuizRewardAsync(
    [Description("The Quiz Command that will give the reward.")] string quizCommand,
    [Description("The order of the entry in the list.")] int sort,
    [Description("The name of the quiz that users will select to start the quiz.")] string name,
    [Description("The reward to give.")] Snowflake reward,
    [Description("The cooldown of the quiz.")] TimeSpan cooldown)
    {
        // error checking on guild id access
        await _database.AddGuildQuizReward(_context.Interaction.GuildID.Value.Value, sort, quizCommand, name, reward.Value, cooldown);
        return (Result)await _feedbackService.SendContextualAsync($":white_check_mark: **Success:** Created role reward <@&{reward.Value}> for ``{quizCommand}``.", ct: CancellationToken);
    }

    [Command("add-user-cooldown")]
    [Description("(debug) add user cooldown")]
    [DiscordDefaultMemberPermissions(DiscordPermission.Administrator)]
    [Ephemeral]
    public async Task<IResult> AddUserCooldownAsync(
    [Description("The user")] Snowflake reward,
    [Description("The role")] Snowflake role,
    [Description("The cooldown of the quiz.")] TimeSpan cooldown)
    {
        // error checking on guild id access
        await _database.AddUserConfig(reward.Value);
        Console.WriteLine($"{reward.Value} {role.Value} {cooldown}");
        await _database.AddUserQuizCooldown(reward.Value, role.Value, cooldown);
        return (Result)await _feedbackService.SendContextualAsync($":white_check_mark: **Success:** Created Cooldown.", ct: CancellationToken);
    }

    [Command("delete-quiz-reward")]
    [Description("Delete a Kotoba quiz reward.")]
    [DiscordDefaultMemberPermissions(DiscordPermission.Administrator)]
    [Ephemeral]
    public async Task<IResult> RemoveGuildQuizRewardAsync(
    [Description("The Quiz Command that will give the reward.")] string quiz)
    {
        // error checking on guild id access
        await _database.RemoveGuildQuizReward(_context.Interaction.GuildID.Value.Value, _database.QuizRewards.Where(x => x.Name == quiz).First().Id);
        return (Result)await _feedbackService.SendContextualAsync($":white_check_mark: **Success:** Deleted role reward for ``{quiz}``.", ct: CancellationToken);
    }

    [Command("list-quiz-rewards")]
    [Description("List Kotoba quiz rewards.")]
    [Ephemeral]
    public async Task<IResult> ListGuildQuizRewardsAsync()
    {
        // error checking on guild id access
        GuildConfig conf = await _database.GuildConfigs.FindAsync(_context.Interaction.GuildID.Value.Value);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("-# Run the listed Kotoba command to have a chance at getting one of these roles!");
        List<QuizReward> test = new List<QuizReward>();

        foreach (var entry in conf.QuizRewards)
        {
            test.Add(_database.QuizRewards.Where(x => x.Id == entry).First());
        }

        test.Sort((x, y) => x.Sort.CompareTo(y.Sort));

        foreach (var entry in test)
        {
            sb.AppendLine($"<@&{entry.Id}> -> ``/quiz {entry.Name}``");
        }
        return (Result)await _feedbackService.SendContextualAsync(sb.ToString(), ct: CancellationToken);
    }

    [Command("onboard")]
    [Description("(debug) onboard the server")]
    //[DiscordDefaultMemberPermissions(DiscordPermission.Administrator)]
    [Ephemeral]
    public async Task<IResult> DebugOnboardServerAsync()
    {
        await _database.AddGuildConfig(_context.Interaction.GuildID.Value.Value);
        return (Result)await _feedbackService.SendContextualAsync("Attempted to onboard server", ct: CancellationToken);
    }

    [Command("set-notification-channel")]
    [Description("Set the public server notifications channel for events like quiz passing")]
    [DiscordDefaultMemberPermissions(DiscordPermission.Administrator)]
    [Ephemeral]
    public async Task<IResult> SetGuildNotificationChannelAsync(
        [Description("The notification channel to set.")] Snowflake channel)
    {
        if (_context.TryGetGuildID(out var guildId))
            await _database.SetGuildNotificationChannel(guildId.Value, channel.Value);
        return (Result)await _feedbackService.SendContextualAsync("Attempted to set notification channel", ct: CancellationToken);
    }

    [Command("add-quiz-channel")]
    [Description("Add a reward for completing a Kotoba quiz.")]
    [DiscordDefaultMemberPermissions(DiscordPermission.Administrator)]
    [Ephemeral]
    public async Task<IResult> RecordGuildQuizChannelAsync(
    [Description("The channel to add.")] Snowflake channel)
    {
        // error checking on guild id access
        await _database.AddGuildQuizChannel(_context.Interaction.GuildID.Value.Value, channel.Value);
        return (Result)await _feedbackService.SendContextualAsync($":white_check_mark: **Success:** Tracking quiz channel <#{channel}>.", ct: CancellationToken);
    }
}