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
using FuzzySharp.Edits;
using Remora.Discord.API.Gateway.Commands;
using Remora.Discord.Extensions.Embeds;
using Nishizono.Bot.Gateway.Quiz;
using Nishizono.Bot.Remora;
using Remora.Discord.API.Abstractions.Rest;

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
    private readonly IDiscordRestChannelAPI _channelApi;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaLogCommands"/> class.
    /// </summary>
    /// <param name="feedbackService">The feedback service.</param>
    public MediaLogCommands(FeedbackService feedbackService, IInteractionCommandContext interactionContext, NishizonoDbContext database, IRollcallProvider<IMetadataProvider> provider, IDiscordRestChannelAPI channelApi)
    {
        _feedbackService = feedbackService;
        _channelApi = channelApi;
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

        // show old and new immersion duration
        IQueryable<ImmersionLog> output = (await _database.GetImmersionLogs(_context.Interaction.Member.Value.User.Value.ID.Value, DateTime.SpecifyKind(new DateTime(year: DateTime.UtcNow.Year, month: DateTime.UtcNow.Month, day: 1), DateTimeKind.Utc)));

        List<ImmersionLog> results = output.ToList();

        if (results.Count() == 0) return (Result)await _feedbackService.SendContextualAsync($":no_entry_sign: **Error:** You have no immersion logs to show.", ct: CancellationToken);
        results.Reverse();

        TimeSpan totalTime = new();

        foreach (ImmersionLog logged in results)
        {
            totalTime = totalTime.Add(logged.Duration);
        }

        TimeSpan oldTime = totalTime.Subtract(log.Duration);

        string immersionDurationProgress = $"**{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(DateTime.Now.Month)}:** " +
            $"{Math.Floor(oldTime.TotalHours)}h {oldTime.Minutes}m -> {Math.Floor(totalTime.TotalHours)}h {totalTime.Minutes}m";

        int streak = 1;

        DateTime lastDate = DateTime.UtcNow.Date;

        foreach (ImmersionLog logged in results)
        {
            if (logged.TimeStamp.Date == lastDate) continue;

            if (lastDate.Subtract(TimeSpan.FromDays(1)) == logged.TimeStamp.Date)
            {
                streak++;
            }
            else
            {
                break;
            }
            
            lastDate = logged.TimeStamp.Date;
        }

        streak--;

        EmbedBuilder b = new();
        b.WithTitle(MediaLogEmbedInterpolation.Title(mediaType, amount, result.Title));
        b.WithDescription((content != "@") ? $"[{result.Title}]({result.Url})\n{immersionDurationProgress}" : $"{immersionDurationProgress}");
        b.WithColour(_feedbackService.Theme.Success);
        if (content != "@")
        {
            b.WithThumbnailUrl(result.Image);
        }
        b.AddField(new EmbedField("Streak", $"{streak} days"));
        b.WithFooter(new MediaLogFooter(_context.Interaction.Member.Value.User.Value));
        var embed = b.Build().Get();

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
        if(output.Count() == 0) return (Result)await _feedbackService.SendContextualAsync($":no_entry_sign: **Error:** You have no immersion logs to remove.", ct: CancellationToken, options: new(MessageFlags: MessageFlags.Ephemeral));
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

        if(results.Count() == 0) return (Result)await _feedbackService.SendContextualAsync($":no_entry_sign: **Error:** You have no immersion logs to show.", ct: CancellationToken, options: new(MessageFlags: MessageFlags.Ephemeral));
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

    [Command("me")]
    [Description("Get an overview of your immersion progress")]
    [SuppressInteractionResponse(true)]
    public async Task<IResult> PostImmersionProgressAsync(
        [Description("Start reporting from a given date")] string since = "")
    {
        DateTime sinceDate;

        if (since == "")
        {
            sinceDate = new DateTime(year: DateTime.UtcNow.Year, month: DateTime.UtcNow.Month, day: 1);
        }
        else
        {
            if (!DateTime.TryParse(since, out sinceDate))
            {
                return (Result)await _feedbackService.SendContextualAsync($":no_entry_sign: **Error:** The provided date was in an invalid format.", ct: CancellationToken);
            }
        }

        IQueryable<ImmersionLog> output = (await _database.GetImmersionLogs(_context.Interaction.Member.Value.User.Value.ID.Value, DateTime.SpecifyKind(sinceDate, DateTimeKind.Utc)));

        List<ImmersionLog> results = output.ToList();

        if (results.Count() == 0) return (Result)await _feedbackService.SendContextualAsync($":no_entry_sign: **Error:** You have no immersion logs to show.", ct: CancellationToken);
        results.Reverse();

        TimeSpan totalTime = new();

        int vn = 0, manga = 0, anime = 0, book = 0, listening = 0, youtube = 0, anki = 0;

        foreach (ImmersionLog result in results)
        {
            totalTime = totalTime.Add(result.Duration);

            switch (result.MediaType)
            {
                case MediaType.VisualNovel:
                    vn += result.Amount;
                    break;
                case MediaType.Manga:
                    manga += result.Amount;
                    break;
                case MediaType.Anime:
                    anime += result.Amount;
                    break;
                case MediaType.Book:
                    book += result.Amount;
                    break;
                case MediaType.Listening:
                    listening += result.Amount;
                    break;
                case MediaType.Youtube:
                    youtube += result.Amount;
                    break;
                case MediaType.Anki:
                    anki += result.Amount;
                    break;
            }
        }

        EmbedBuilder b = new();
        b.WithTitle($"Immersion Overview since {sinceDate.ToShortDateString()}");
        b.WithDescription($"**Anime: ** {anime} episodes\n" +
                         $"**Anki: ** {anki} cards\n" +
                         $"**Books: ** {book} chars\n" +
                         $"**Manga: ** {manga} pages\n" +
                         $"**Listening: ** {listening} minutes\n" +
                         $"**Visual Novels: ** {vn} chars\n" +
                         $"**YouTube: ** {youtube} videos\n\n" +
                         $"**Total Time: {Math.Floor(totalTime.TotalHours)}h {totalTime.Minutes}m**");
        b.WithColour(_feedbackService.Theme.Success);
        b.WithImageUrl("attachment://image.png");
        b.WithFooter(new MediaLogFooter(_context.Interaction.Member.Value.User.Value));
        var embed = b.Build().Get();

        _context.TryGetChannelID(out var channel);

        await _channelApi.CreateMessageAsync(
            channel,
            attachments: new([new FileData("image.png", ImmersionPlotter.PlotImmersionLogs(results))]),
            embeds: new([embed]));

        return (Result)await _feedbackService.SendContextualAsync("-# Check the [website](https://www.example.com/) for more detail!", ct: CancellationToken, options: new(MessageFlags: MessageFlags.Ephemeral));
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