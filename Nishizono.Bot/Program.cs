using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Remora.Commands.Extensions;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Gateway.Commands;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway;
using Remora.Discord.Hosting.Extensions;
using Remora.Rest.Core;
using Nishizono.Bot.Commands;
using Nishizono.Database;
using Remora.Discord.Gateway.Extensions;
using Nishizono.Bot.Gateway;
using Remora.Discord.Gateway.Responders;
using Nishizono.Providers;
using Rollcall.Extensions.Microsoft.DependencyInjection;
using GraphQL.Client.Serializer.SystemTextJson;
using Remora.Discord.API.Abstractions.Objects;
using GraphQL.Client.Http;
using Nishizono.Bot.Gateway.Quiz;

namespace Nishizono.Bot;

/// <summary>
/// Represents the main class of the program.
/// </summary>
public class Program
{
    /// <summary>
    /// The main entry point of the program.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous program execution.</returns>
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args)
            .Build();

        var services = host.Services;
        var log = services.GetRequiredService<ILogger<Program>>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var database = services.GetRequiredService<NishizonoDbContext>();
        await database.Database.EnsureCreatedAsync();

        #if DEBUG
                Snowflake? debugServer = null;
                var debugServerString = configuration.GetValue<string?>("REMORA_DEBUG_SERVER");
                if (debugServerString is not null)
                {
                    if (!DiscordSnowflake.TryParse(debugServerString, out debugServer))
                    {
                        log.LogWarning("Failed to parse debug server from environment");
                    }
                }
        #endif

        var slashService = services.GetRequiredService<SlashService>();
        var updateSlash = await slashService.UpdateSlashCommandsAsync();
        if (!updateSlash.IsSuccess)
        {
            log.LogWarning("Failed to update slash commands: {Reason}", updateSlash.Error.Message);
        }

        await host.RunAsync();
    }

    /// <summary>
    /// Creates a generic application host builder.
    /// </summary>
    /// <param name="args">The arguments passed to the application.</param>
    /// <returns>The host builder.</returns>
    private static IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args)
        .AddDiscordService
        (
            services =>
            {
                var configuration = services.GetRequiredService<IConfiguration>();
                return configuration.GetValue<string?>("REMORA_BOT_TOKEN") ??
                       throw new InvalidOperationException
                       (
                           "No bot token has been provided. Set the REMORA_BOT_TOKEN environment variable to a " +
                           "valid token."
                       );
            }
        )
        .ConfigureServices
        (
            (_, services) =>
            {
                services.AddDbContext<NishizonoDbContext>();
                services.AddSingleton<QuizManager>();
                services.AddNamedService<IMetadataProvider>(builder => builder
                
                    .AddTransient("vndb", typeof(VndbMetadataProvider))
                    .AddTransient("anilist", typeof(AnilistMetadataProvider))
                    .AddTransient("youtube", typeof(YouTubeMetadataProvider))
                );
                services.AddAutocompleteProvider<MediaAutoCompleteProvider>();
                services.AddResponder<QuizResponder>(ResponderGroup.Normal);
                services.AddResponder<GuildJoinResponder>(ResponderGroup.Normal);
                services.Configure<DiscordGatewayClientOptions>(g => g.Intents |= GatewayIntents.MessageContents);
                services
                    .AddDiscordCommands(true)
                    .AddCommandTree()
                        .WithCommandGroup<GuildConfigCommands>()
                        .WithCommandGroup<QuizCommands>()
                        .WithCommandGroup<MediaLogCommands>();
            }
        )
        .ConfigureLogging
        (
            c => c
                .AddConsole()
                .AddFilter("System.Net.Http.HttpClient.*.LogicalHandler", LogLevel.Warning)
                .AddFilter("System.Net.Http.HttpClient.*.ClientHandler", LogLevel.Warning)
        );
}
