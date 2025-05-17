using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using Nishizono.Database.Models;
using System.Threading.Channels;
using System;

namespace Nishizono.Database;

public partial class NishizonoDbContext
{
    /// <summary>
    /// Attempt to add a Guild Configuration entry to the database for the specified Guild.
    /// </summary>
    /// <param name="guild">The Guild ID to add an entry for.</param>
    /// <returns>A bool indicating whether or not the guild configuration was successfully added.</returns>
    public async Task<bool> AddGuildConfig(ulong guild)
    {
        GuildConfig? config = await GuildConfigs.FindAsync(guild);

        if (config is null)
        {
            GuildConfigs.Add(new()
            {
                Id = guild,
                ImmersionLoggingEnabled = false,
                ImmersionLoggingPublic = false,
                ImmersionLoggingChannel = 0,
                QuizChannels = new List<ulong>(),
                QuizRewards = new List<ulong>(),
                NotificationChannel = 0,
            });
            await SaveChangesAsync();

            _logger.LogInformation($"AddGuildConfig: Added guild {guild}");
            return true;
        }

        _logger.LogWarning($"AddGuildConfig: a configuration was already present for guild {guild}");
        return false;
    }

    /// <summary>
    /// Attempt to get the GuildConfig for the given guild.
    /// </summary>
    /// <param name="guild">The Guild ID to add the relation to.</param>
    /// <returns>Returns the GuildConfig for the given Guild ID, or null.</returns>
    public async Task<GuildConfig?> GetGuildConfig(ulong guild)
    {
        return await GuildConfigs.FindAsync(guild);
    }

    /// <summary>
    /// Attempt to add a Quiz Reward relation for the specified Guild.
    /// </summary>
    /// <param name="guild">The Guild ID to add the relation to.</param>
    /// <param name="sort">The sort priority of the entry.</param>
    /// <param name="quiz">The Quiz String of the Quiz Reward relation.</param>
    /// <param name="role">The Role ID of the Quiz Reward relation.</param>
    /// <param name="cooldown">The cooldown interval that will be applied to attempts of quizzes started by quizString.</param>
    /// <returns>A bool indicating whether or not the quiz reward relation was successfully added.</returns>
    public async Task<bool> AddGuildQuizReward(ulong guild, int sort, string quiz, string name, ulong role, TimeSpan cooldown)
    {
        return await ModifyGuildConfig(guild, _ =>
        {
            QuizRewards.Add(new QuizReward
            {
                Command = quiz,
                Sort = 0,
                Id = role,
                Cooldown = cooldown,
                Name = name
            });

            _.QuizRewards.Add(role);
        });
    }

    /// <summary>
    /// Attempt to remove a Quiz Reward relation from the specified Guild.
    /// </summary>
    /// <param name="guild">The Guild ID to remove the relation from.</param>
    /// <param name="role">The Role ID of the Quiz Reward relation.</param>
    /// <returns>A bool indicating whether or not the quiz reward relation was successfully removed.</returns>
    public async Task<bool> RemoveGuildQuizReward(ulong guild, ulong role)
    {
        return await ModifyGuildConfig(guild, async _ =>
        {
            QuizReward? reward = await QuizRewards.FindAsync(role);

            if (reward is not null)
            {
                QuizRewards.Remove(reward);
                _.QuizRewards.Remove(role);
            }
        });
    }

    /// <summary>
    /// Attempt to set the Quiz Channel entry to the database for the specified Guild Configuration.
    /// </summary>
    /// <param name="guild">The Guild ID to add a Quiz channel entry to.</param>
    /// <param name="channel">The Channel ID to set.</param>
    /// <returns>A bool indicating whether or not the quiz channel was successfully added.</returns>
    public async Task<bool> AddGuildQuizChannel(ulong guild, ulong channel)
    {
        return await ModifyGuildConfig(guild, _ => { _.QuizChannels.Add(channel); });
    }

    /// <summary>
    /// Attempt to set the Notifications Channel entry to the database for the specified Guild Configuration.
    /// </summary>
    /// <param name="guild">The Guild ID to set the Notification Channel entry of.</param>
    /// <param name="channel">The Channel ID to set.</param>
    /// <returns>A bool indicating whether or not the notification channel was successfully set.</returns>
    public async Task<bool> SetGuildNotificationChannel(ulong guild, ulong channel)
    {
        return await ModifyGuildConfig(guild, _ => { _.NotificationChannel = channel; });
    }

    /// <summary>
    /// Attempt to set the Immersion Logging Channel entry to the database for the specified Guild Configuration.
    /// </summary>
    /// <param name="guild">The Guild ID to set the Immersion Logging Channel entry of.</param>
    /// <param name="channel">The Channel ID to set.</param>
    /// <returns>A bool indicating whether or not the immersion logging channel was successfully set.</returns>
    public async Task<bool> SetGuildImmersionLoggingChannel(ulong guild, ulong channel)
    {
        return await ModifyGuildConfig(guild, _ => { _.ImmersionLoggingChannel = channel; });
    }

    /// <summary>
    /// Attempt to modify the Guild Configuration entry in the database for the given Guild ID.
    /// Changes made to the configuration will be saved to the database context after successful execution of the modification.
    /// </summary>
    /// <param name="guild">The Guild ID to modify.</param>
    /// <param name="modification">The operation to modify the Guild Configuration with.</param>
    /// <returns>A bool indicating whether or not the modification was applied to the Guild Config.</returns>
    private async Task<bool> ModifyGuildConfig(ulong guild, Action<GuildConfig> modification)
    {
        GuildConfig? config = await GuildConfigs.FindAsync(guild);

        if (config is not null)
        {
            EntityEntry entry = GuildConfigs.Entry(config);
            modification.Invoke(config);
            entry.CurrentValues.SetValues(config);
            await SaveChangesAsync();

            _logger.LogInformation($"ModifyGuildConfig: {modification.Method} succeeded");
            return true;
        }

        _logger.LogWarning($"ModifyGuildConfig: called ({modification.Method.ReturnType.Name}) {modification.Method.Name} for {guild}, " +
            $"but a configuration was not present for the guild");

        return false;
    }
}