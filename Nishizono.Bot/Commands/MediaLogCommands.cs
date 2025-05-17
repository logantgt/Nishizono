using System;
using System.ComponentModel;
using System.Net;
using System.Threading.Tasks;
using CommunityToolkit.HighPerformance.Helpers;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Parsers;
using Remora.Rest.Core;
using Remora.Results;
using Nishizono.Database;
using Nishizono.Database.Models;
using OneOf.Types;
using System.Globalization;
using System.Text;
using Nishizono.Providers;
using Rollcall.Extensions.Microsoft.DependencyInjection;

/// <summary>
/// Responds to Media Logging commands.
/// </summary>
namespace Nishizono.Bot.Commands;
public class MediaLogCommands : CommandGroup
{
    private readonly FeedbackService _feedbackService;
    private readonly IInteractionCommandContext _context;
    private readonly NishizonoDbContext _database;
    private readonly IRollcallProvider<IMetadataProvider> _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaLogCommands"/> class.
    /// </summary>
    /// <param name="feedbackService">The feedback service.</param>
    public MediaLogCommands(FeedbackService feedbackService, IInteractionCommandContext interactionContext, NishizonoDbContext database, IRollcallProvider<IMetadataProvider> provider)
    {
        _feedbackService = feedbackService;
        _context = interactionContext;
        _database = database;
        _provider = provider;
    }

    [Command("log")]
    [Description("Log your immersion.")]
    [SuppressInteractionResponse(true)]
    public async Task<IResult> RecordImmersionLogAsync(
        [Description("The type of media being logged.")] MediaType mediaType,
        [Description("Episodes watched, characters or pages read, cards reviewed.")] int amount,
        [Description("Time spent on the activity, as [hh:mm] or [mm]")] TimeSpan duration,
        [Description("vndb IDs and titles for VNs, Anilist codes for Anime and Manga.")][AutocompleteProvider("autocomplete::media")] string content,
        [Description("Comment must be no more than 50 chararacters long.")] string comment = "")
    {
        if (comment.Length > 50) return (Result)await _feedbackService.SendContextualAsync(":no_entry_sign: **Error:** Comment must be no more than 50 characters long.", ct: CancellationToken, options: new(MessageFlags: MessageFlags.Ephemeral));
        if (mediaType == MediaType.Youtube && amount != 1) return (Result)await _feedbackService.SendContextualAsync(":no_entry_sign: **Error:** You may only log one YouTube video at a time.", ct: CancellationToken, options: new(MessageFlags: MessageFlags.Ephemeral));

        ProviderMetadata result;

        switch (mediaType)
        {
            case MediaType.VisualNovel:
                result = (await _provider.GetService("vndb").QueryAsync(MetadataType.VisualNovel, QueryType.Id, content, true)).First();
                break;
            case MediaType.Anime:
                result = (await _provider.GetService("anilist").QueryAsync(MetadataType.Anime, QueryType.Id, content, true)).First();
                break;
            case MediaType.Manga:
                result = (await _provider.GetService("anilist").QueryAsync(MetadataType.Manga, QueryType.Id, content, true)).First();
                break;
            case MediaType.Youtube:
                result = (await _provider.GetService("youtube").QueryAsync(MetadataType.Youtube, QueryType.Id, content, true)).First();
                break;
            default:
                result = new ProviderMetadata() { Id = 0, Title = content, NativeTitle = "", Image = "", ProviderId = "", QueryId = 0, Url = "" }; content = "@";
                break;
        }

        // TODO: Safely unwrap member and user values to get snowflake
        ImmersionLog log = new()
        {
            Id = 0,
            UserId = _context.Interaction.Member.Value.User.Value.ID.Value,
            GuildId = _context.Interaction.GuildID.Value.Value,
            MediaType = mediaType,
            Amount = amount,
            Interpolated = false,
            Duration = duration,
            TimeStamp = DateTime.UtcNow,
            Title = result.Title,
            Content = content,
            Comment = comment,
        };
        await _database.AddImmersionLog(log);

        var embed = new Embed(
            Type: EmbedType.Video,
            Title: MediaLogEmbedInterpolation.Title(mediaType, amount, result.Title),
            Colour: _feedbackService.Theme.Success,
            Description: (content != "@") ? $"[{result.Title}]({result.Url})" : "",
            Thumbnail: (content != "@") ? new EmbedThumbnail(result.Image) : null,
            Footer: new MediaLogFooter(_context.Interaction.Member.Value.User.Value));

        if (mediaType == MediaType.Youtube)
        {
            await _feedbackService.SendContextualEmbedAsync(embed);
            return (Result)await _feedbackService.SendContextualAsync($"> {result.Url}", ct: CancellationToken);
        }
        else
        {
            return (Result)await _feedbackService.SendContextualEmbedAsync(embed);
        }
    }

    [Command("undo")]
    [Description("Delete your last immersion log.")]
    [Ephemeral]
    public async Task<IResult> UndoImmersionLogAsync()
    {
        IQueryable<ImmersionLog> output = (await _database.GetImmersionLogs(_context.Interaction.Member.Value.User.Value.ID.Value)).OrderBy(_ => _.Id);
        if(output.Count() == 0) return (Result)await _feedbackService.SendContextualAsync($":no_entry_sign: **Error:** You have no immersion logs to remove.", ct: CancellationToken);
        var lastOutput = output.Last();
        await _database.RemoveImmersionLog(lastOutput);

        if (!String.IsNullOrEmpty(lastOutput.Comment)) lastOutput.Comment = " comment:" + lastOutput.Comment;
        if (lastOutput.Content == "@") lastOutput.Content = lastOutput.Title;

        return (Result)await _feedbackService.SendContextualAsync(
            $":white_check_mark: **Success:** Removed your latest immersion log.\n" +
            $"-# If you want to restore this log, use the command " +
            $"``/log mediatype:{lastOutput.MediaType} amount:{lastOutput.Amount} " +
            $"duration:{lastOutput.Duration} content:{lastOutput.Content}{lastOutput.Comment}``", 
            ct: CancellationToken);
    }

    [Command("logs")]
    [Description("View your immersion logs.")]
    [SuppressInteractionResponse(true)]
    public async Task<IResult> PostImmersionLogsAsync()
    {
        IQueryable<ImmersionLog> output = (await _database.GetImmersionLogs(_context.Interaction.Member.Value.User.Value.ID.Value));

        List<ImmersionLog> results = output.ToList();

        if(results.Count() == 0) return (Result)await _feedbackService.SendContextualAsync($":no_entry_sign: **Error:** You have no immersion logs to show.", ct: CancellationToken);
        results.Reverse();

        StringBuilder logs = new();
        logs.AppendLine("```ps");
        foreach (ImmersionLog result in results)
        {
            if (result.Comment == "") result.Comment = "No comment";
            logs.AppendLine($"{result.TimeStamp.Date.ToString("yyyy-MM-dd")}: [{result.MediaType}][{result.Content}] \"{result.Title}\" -> {result.Amount}, {result.Duration} # {result.Comment}");
        }
        logs.AppendLine("```");

        return (Result)await _feedbackService.SendContextualAsync(logs.ToString(), ct: CancellationToken);
    }
}

public static class MediaLogEmbedInterpolation
{
    public static string Title(MediaType mediaType, int amount, string content)
    {
        string s = amount > 1 ? "s" : "";

        switch (mediaType)
        {
            case MediaType.VisualNovel:
                return $"Read {amount} char{s} of {content}!";
            case MediaType.Manga:
                return $"Read {amount} page{s} of {content}!";
            case MediaType.Anime:
                return $"Watched {amount} episode{s} of {content}!";
            case MediaType.Book:
                return $"Read {amount} char{s} of {content}!";
            case MediaType.Listening:
                return $"Logged {amount} session{s} of {content}!";
            case MediaType.Anki:
                return $"Reviewed {amount} card{s} of {content}!";
            case MediaType.Youtube:
                return $"Watched {content}!";
            default:
                return $"Logged {amount} of {content}!";
        }
    }
}

public class MediaLogFooter : IEmbedFooter
{
    string _text = "";
    string _iconUrl = "";

    public MediaLogFooter(IUser user)
    {
        _iconUrl = CDN.GetUserAvatarUrl(user).Entity.ToString();
        _text = $"From {user.Username} on {DateTime.UtcNow.Date.ToShortDateString()}";
    }

    public string Text => _text;

    public Optional<string> IconUrl => new(_iconUrl);

    public Optional<string> ProxyIconUrl => new(_iconUrl);
}