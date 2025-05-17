using Nishizono.Database;
using Nishizono.Database.Models;
using Nishizono.Bot.Remora;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Extensions.Embeds;
using Remora.Rest.Core;
using Remora.Results;
using System.Composition;
using System.Reflection;

namespace Nishizono.Bot.Gateway.Quiz;

/// <summary>
/// The management module for quiz functionality.
/// This is intended to be injected into the DI container once per the lifetime of the application.
/// </summary>
internal class QuizManager
{
    private readonly List<QuizDeck> _decks;
    private readonly Dictionary<Snowflake, QuizSession> _sessions;
    private readonly IDiscordRestGuildAPI _guildApi;
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly QuizRenderer _quizRenderer;
    private readonly NishizonoDbContext _database;
    public QuizManager(NishizonoDbContext database, IDiscordRestGuildAPI guildApi, IDiscordRestChannelAPI channelApi)
    {
        _guildApi = guildApi;
        _channelApi = channelApi;
        _decks = new();
        _sessions = new();
        _quizRenderer = new QuizRenderer();
        _database = database;
        LoadDecks("decks");
    }

    /// <summary>
    /// Register a quiz session in this QuizManager.
    /// </summary>
    /// <param name="channelId">The Snowflake-derived Channel ID of the channel the quiz is taking place in.</param>
    /// <param name="guildId">The Snowflake-derived Channel ID of the guild the quiz is taking place in.</param>
    /// <param name="quizString">The string that the quiz was initiated in.</param>
    public void RegisterQuiz(QuizSessionOptions opts, Snowflake channelId, Snowflake guildId, string quizString)
    {
        _sessions.Add(channelId, new QuizSession(opts,
            channelId,
            guildId,
            this,
            quizString));
    }

    /// <summary>
    /// Load decks from the specified path into this Quiz Manager's deck store.
    /// </summary>
    /// <param name="path">The path to load the quizzes from.</param>
    private void LoadDecks(string path)
    {
        foreach (string folder in Directory.EnumerateDirectories(path))
        {
            if (File.Exists(Path.Combine(folder, "meta.json")) && File.Exists(Path.Combine(folder, "deck.json")))
            {
                _decks.Add(new QuizDeck(folder));
            }
        }
    }

    /// <summary>
    /// The main loop in which Quiz logic is ran upon a given quiz session.
    /// </summary>
    /// <param name="session">The session to run.</param>
    /// <returns></returns>
    public async Task DoQuiz(QuizSession session)
    {
        while (session.Finished == false)
        {
            bool wait = true;
            bool answered = false;
            bool skipped = false;
            bool next = session.GetNextCard();
            int slept = 0;

            if (session.CurrentCard.Render == "Image")
            {
                await _channelApi.CreateMessageAsync(
                    session.ChannelId,
                    attachments: new([_quizRenderer.RenderToRemoraAttachment(session.CurrentCard.Question, session.RenderEffect)]),
                    embeds: new([QuizEmbeds.BuildQuizCardEmbed(session)]));
            }
            else
            {
                await _channelApi.CreateMessageAsync(
                    session.ChannelId,
                    embeds: new([QuizEmbeds.BuildQuizCardEmbed(session)]));
            }

                // TODO: please find a better way of polling a collection for changes and waiting the card duration
                while (wait)
                {
                    await Task.Delay(50);
                    slept += 50;
                    if (session.Responses.Count > 0)
                    {
                        // automatically skip if hardcore is enabled
                        if (session.Hardcore) skipped = true;
                        foreach (var response in session.Responses)
                        {
                            if (session.CurrentCard.Answers.Contains(response.Item2))
                            {
                                answered = true;
                                session.Participants[response.Item1].AddPoint(session.CurrentDeck);
                                if (session.Participants[response.Item1].GetPoints(session.CurrentDeck) == session.SessionDecks[session.CurrentDeck].Limit)
                                {
                                    session.SessionDecks[session.CurrentDeck].Shut();
                                }
                                break;
                            }

                            if (response.Item2 == "skip" || response.Item2 == "s" || response.Item2 == "。")
                            {
                                skipped = true;
                                break;
                            }
                        }

                        if (answered || skipped) break;
                    }

                    if (slept >= session.CurrentDeck.Metadata.Time) break;
                }

            if (answered)
            {
                session.SetCurrentCorrectUser(session.Responses.First().Item1);
                await _channelApi.CreateMessageAsync(session.ChannelId, embeds: new([QuizEmbeds.BuildQuizAnswerEmbed(session)]));

                // TODO: make sure this makes sense
                bool complete = false;

                foreach (var score in session.Participants[session.Responses.First().Item1].Scores) // Scores always returns both, not the one that was answered.
                {
                    complete = score.Value == session.SessionDecks[score.Key].Limit;
                    if (complete == false) break;
                }

                if (complete)
                {
                    session.SetWinner(session.Responses.First().Item1);
                    session.Finish();
                }

                session.SetCurrentCorrectUser(new Snowflake(0));
                session.ClearResponses();
            }
            else
            {
                await _channelApi.CreateMessageAsync(session.ChannelId, embeds: new([QuizEmbeds.BuildQuizAnswerEmbed(session)]));
                session.UnansweredQuestions.Add(session.CurrentCard.Question);
                if (session.UnansweredQuestions.Count >= session.FailLimit) session.Finish();
                session.ClearResponses();
            }
        }
        if(session.Winner.Value != 0) await RewardRole(session);
        _sessions.Remove(session.ChannelId);
        await _channelApi.CreateMessageAsync(session.ChannelId, embeds: new([QuizEmbeds.BuildQuizFinishedEmbed(session)]));
    }

    /// <summary>
    /// Reward roles for the given quiz session, if appropriate.
    /// </summary>
    /// <param name="session">The quiz session to evaluate.</param>
    /// <returns></returns>
    private async Task<Result> RewardRole(QuizSession session)
    {
        // read the guild config for this message
        GuildConfig? conf = await _database.GuildConfigs.FindAsync(session.GuildId.Value);

        foreach (ulong i in conf.QuizRewards)
        {
            var r = _database.QuizRewards.Where(x => x.Id == i).FirstOrDefault();
            if (r.Command == session.QuizString)
            {
                Snowflake.TryParse($"{r.Id}", out var roleId);
                Snowflake.TryParse($"{conf.Id}", out var guildId);
                var config = await _database.GetGuildConfig(guildId.Value.Value);
                Snowflake.TryParse($"{config.NotificationChannel}", out var notificationChannelId);

                Console.WriteLine(config.NotificationChannel);

                await _channelApi.CreateMessageAsync((Snowflake)notificationChannelId, $"<@{session.Winner}> has successfully completed the <@&{roleId}> quiz!");

                return await _guildApi.AddGuildMemberRoleAsync((Snowflake)guildId, session.Winner, (Snowflake)roleId, ct: CancellationToken.None);
            }
        }

        return Result.FromSuccess();
    }

    public List<QuizDeck> Decks { get => _decks; }
    public Dictionary<Snowflake, QuizSession> Sessions { get => _sessions; }
}

public enum QuizDeckFormat
{
    Basic,
    Multi
}
public enum QuizCardRender
{
    Text,
    Image
}