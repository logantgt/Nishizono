using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using Nishizono.Database.Models;
using System.Data;

namespace Nishizono.Database;

public partial class NishizonoDbContext
{
    /// <summary>
    /// Attempt to add a User Configuration entry to the database for the specified User.
    /// </summary>
    /// <param name="userId">The User ID to add an entry for.</param>
    /// <returns>A bool indicating whether or not the user configuration was successfully added.</returns>
    public async Task<bool> AddUserConfig(ulong userId)
    {
        UserConfig? config = await UserConfigs.FindAsync(userId);

        if (config is null)
        {
            UserConfigs.Add(new()
            {
                Id = userId,
                QuizCooldowns = new Dictionary<string, string>()

            });

            await SaveChangesAsync();

            _logger.LogInformation($"Added User Config for {userId}");
            return true;
        }
        _logger.LogWarning($"AddUserConfig was called for {userId}, " +
            $"but a configuration was already present for the user");
        return false;
    }

    /// <summary>
    /// Attempt to get the UserConfig for a specified user ID.
    /// </summary>
    /// <param name="user">The User ID to get the configuration for.</param>
    /// <returns>A bool indicating whether or not the quiz cooldown was successfully added.</returns>
    public async Task<UserConfig> GetUserConfig(ulong user)
    {
        return UserConfigs.Where(_ => _.Id == user).First();
    }

    /// <summary>
    /// Attempt to add a Quiz Cooldown entry onto the specified User's configuration.
    /// </summary>
    /// <param name="user">The User ID to add the cooldown to.</param>
    /// <param name="role">The Role ID from the reward that the cooldown affects.</param>
    /// <param name="cooldown">The duration of the cooldown restriction.</param>
    /// <returns>A bool indicating whether or not the quiz cooldown was successfully added.</returns>
    public async Task<bool> AddUserQuizCooldown(ulong user, ulong role, TimeSpan cooldown)
    {
        return await ModifyUserConfig(user, _ =>
        {
            _.QuizCooldowns.Add($"{role}", $"{DateTime.UtcNow.Add(cooldown)}");
        });
    }

    /// <summary>
    /// Attempt to remove a Quiz Cooldown entry from the specified User's configuration.
    /// </summary>
    /// <param name="user">The User ID to remove the cooldown from.</param>
    /// <param name="role">The Role ID from the reward that the cooldown affects.</param>
    /// <returns>A bool indicating whether or not the quiz cooldown was successfully removed.</returns>
    public async Task<bool> RemoveUserQuizCooldown(ulong user, ulong role)
    {
        return await ModifyUserConfig(user, _ =>
        {
            _.QuizCooldowns.Remove($"{role}");
        });
    }

    /// <summary>
    /// Attempt to modify the User Configuration entry in the database for the given User ID.
    /// Changes made to the configuration will be saved to the database context after successful execution of the modification.
    /// </summary>
    /// <param name="user">The User ID to modify.</param>
    /// <param name="modification">The operation to modify the User Configuration with.</param>
    /// <returns>A bool indicating whether or not the modification was applied to the User Config.</returns>
    private async Task<bool> ModifyUserConfig(ulong user, Action<UserConfig> modification)
    {
        UserConfig? config = await UserConfigs.FindAsync(user);

        if (config is not null)
        {
            EntityEntry entry = UserConfigs.Entry(config);
            modification.Invoke(config);
            entry.CurrentValues.SetValues(config);
            await SaveChangesAsync();

            _logger.LogInformation($"ModifyUserConfig: {modification.Method} succeeded");
            return true;
        }

        _logger.LogWarning($"ModifyUserConfig: called ({modification.Method.ReturnType.Name}) {modification.Method.Name} for {user}, " +
            $"but a configuration was not present for the user");
        return false;
    }
}
