using CommandLine;
using CommandLine.Text;
using FuzzySharp.Edits;
using Newtonsoft.Json;
using Nishizono.Database;
using Nishizono.Database.Models;
using Nishizono.Bot.Gateway.Quiz;
using OneOf.Types;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Extensions.Embeds;
using Remora.Rest.Core;
using Remora.Results;
using System.ComponentModel;
using System.Text;

namespace Nishizono.Bot.Commands;

internal class QuizCommands : CommandGroup
{
    private readonly FeedbackService _feedbackService;
    private readonly IInteractionCommandContext _context;
    private readonly NishizonoDbContext _database;
    private readonly QuizManager _quizManager;
    private readonly QuizResponder _quizResponder;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaLogCommands"/> class.
    /// </summary>
    /// <param name="feedbackService">The feedback service.</param>
    public QuizCommands(FeedbackService feedbackService, IInteractionCommandContext interactionContext, NishizonoDbContext database, QuizManager quizManager, QuizResponder responder)
    {
        _feedbackService = feedbackService;
        _context = interactionContext;
        _database = database;
        _quizManager = quizManager;
        _quizResponder = responder;
    }

    [Command("debug-quiz")]
    [Description("(debug) Initiate a quiz with the specified quiz string")]
    [SuppressInteractionResponse(true)]
    [DiscordDefaultMemberPermissions(DiscordPermission.Administrator)]
    public async Task<IResult> StartQuizAsync(
    [Description("The quiz string to use.")] string quiz)
    {
        bool success = false;
        // error checking on guild id access
        _context.TryGetUserID(out var userID);
        _context.TryGetChannelID(out var channelID);
        _context.TryGetGuildID(out var guildID);

        if (_quizManager.Sessions.ContainsKey(channelID))
        {
            return (Result)await _feedbackService.SendContextualAsync(":no_entry_sign: **Error:** There is already a " +
                "quiz running in this channel! You can't start another one.", ct: CancellationToken, options: new(MessageFlags: MessageFlags.Ephemeral));
        }

        QuizSessionOptions opts = new();

        CommandLine.Parser.Default.ParseArguments<QuizCommandArgs>(quiz.Split(" "))
            .WithParsed(args =>
            {
                if (args.Scores.Count() > 0 && args.Scores.Count() != args.Decks.Count())
                {
                    Console.WriteLine("This should not be allowed! Throw an error later");
                }

                if (args.Timeouts.Count() > 0 && args.Timeouts.Count() != args.Decks.Count())
                {
                    Console.WriteLine("This should not be allowed! Throw an error later");
                }

                // TODO: please don't do this it looks terrible
                int index = 0;

                foreach (string deckId in args.Decks)
                {
                    int limit = 20;
                    int timeout = 0;
                    if (args.Scores.Count() > 0) limit = int.Parse(args.Scores.ElementAt(index));
                    if (args.Timeouts.Count() > 0) timeout = int.Parse(args.Timeouts.ElementAt(index));
                    QuizDeck deck = _quizManager.Decks.Where(_ => _.Metadata.Id == deckId).First();
                    if (timeout > 0) deck.Metadata.Time = timeout * 1000;
                    opts.SessionDecks.Add(deck, new QuizShuffle(deck.Cards.Length) { Limit = limit });

                    if (index == 0)
                    {
                        opts.Title = deck.Metadata.Title;
                    }
                    else
                    {
                        opts.Title += $" + {deck.Metadata.Title}";
                    }

                    index++;
                }
                opts.FailLimit = args.Fail;
                opts.Multiplayer = args.Multiplayer;
                opts.Hardcore = args.Hardcore;
                if (Enum.TryParse(args.Effect, out QuizRenderEffect renderEffect))
                    opts.RenderEffect = renderEffect;

                success = true;
            })
            .WithNotParsed(async errs =>
            {
               await _feedbackService.SendContextualAsync($":no_entry_sign: **Error:** Couldn't parse quiz string!", ct: CancellationToken, options: new(MessageFlags: MessageFlags.Ephemeral));
            });

        if (!success)
        {
            return Result.FromSuccess();
        }

        _quizManager.RegisterQuiz(opts, channelID, guildID, quiz);
        _quizManager.Sessions[channelID].AddParticipant(userID);

        await _feedbackService.SendContextualAsync($":white_check_mark: **Success:** Starting quiz ``{quiz}`` in 5 seconds!", ct: CancellationToken);
        await Task.Delay(5000);

        _quizManager.DoQuiz(_quizManager.Sessions[channelID]);

        return Result.FromSuccess();
    }

    [Command("quiz")]
    [Description("Start a quiz")]
    [SuppressInteractionResponse(true)]
    public async Task<IResult> StartUserQuizAsync(
    [Description("The quiz to start.")] string quiz)
    {
        bool success = false;
        // error checking on guild id access
        _context.TryGetUserID(out var userID);
        _context.TryGetChannelID(out var channelID);
        _context.TryGetGuildID(out var guildID);

        UserConfig userConfig;

        if (!_database.UserConfigs.Contains(_database.UserConfigs.Find(userID.Value)))
        {
            await _database.AddUserConfig(userID.Value);
        }

        userConfig = await _database.GetUserConfig(userID.Value);

        QuizReward? quizReward = _database.QuizRewards.Where(_ => _.Name == quiz).First();

        if (quizReward is not null)
        {
            if (userConfig.QuizCooldowns.ContainsKey(quizReward.Id.ToString()))
            {
                if (DateTime.UtcNow < DateTime.Parse(userConfig.QuizCooldowns[quizReward.Id.ToString()]))
                {
                    return (Result)await _feedbackService.SendContextualAsync($":no_entry_sign: **Error:** Can't start quiz! " +
                        $"You have an active cooldown on this quiz. Try again <t:{
                            new DateTimeOffset(DateTime.SpecifyKind(DateTime.Parse(
                                userConfig.QuizCooldowns[quizReward.Id.ToString()]), DateTimeKind.Utc))
                            .ToUnixTimeSeconds()}:R>.", 
                            ct: CancellationToken, 
                            options: new(MessageFlags: MessageFlags.Ephemeral));
                }
                else
                {
                    await _database.RemoveUserQuizCooldown(userID.Value, quizReward.Id);
                }
            }
        }

        if (_quizManager.Sessions.ContainsKey(channelID))
        {
            return (Result)await _feedbackService.SendContextualAsync(":no_entry_sign: **Error:** There is already a " +
                "quiz running in this channel! You can't start another one.", ct: CancellationToken, options: new(MessageFlags: MessageFlags.Ephemeral));
        }

        await _database.AddUserQuizCooldown(userID.Value, quizReward.Id, quizReward.Cooldown);

        QuizSessionOptions opts = new();

        string quizCommand = "";

        if (_database.QuizRewards.Contains(_database.QuizRewards.Where(_ => _.Name == quiz).First()))
        {
            quizCommand = _database.QuizRewards.Where(_ => _.Name == quiz).First().Command;
        }

        if (quizCommand == "")
        {
            return (Result)await _feedbackService.SendContextualAsync(":no_entry_sign: **Error:** The specified quiz does not exist!", 
                ct: CancellationToken, options: new(MessageFlags: MessageFlags.Ephemeral));
        }

        CommandLine.Parser.Default.ParseArguments<QuizCommandArgs>(quizCommand.Split(" "))
            .WithParsed(args =>
            {
                if (args.Scores.Count() > 0 && args.Scores.Count() != args.Decks.Count())
                {
                    Console.WriteLine("This should not be allowed! Throw an error later");
                }

                if (args.Timeouts.Count() > 0 && args.Timeouts.Count() != args.Decks.Count())
                {
                    Console.WriteLine("This should not be allowed! Throw an error later");
                }

                // TODO: please don't do this it looks terrible
                int index = 0;

                foreach (string deckId in args.Decks)
                {
                    int limit = 20;
                    int timeout = 0;
                    if (args.Scores.Count() > 0) limit = int.Parse(args.Scores.ElementAt(index));
                    if (args.Timeouts.Count() > 0) timeout = int.Parse(args.Timeouts.ElementAt(index));
                    QuizDeck deck = _quizManager.Decks.Where(_ => _.Metadata.Id == deckId).First();
                    if (timeout > 0) deck.Metadata.Time = timeout * 1000;
                    opts.SessionDecks.Add(deck, new QuizShuffle(deck.Cards.Length) { Limit = limit });

                    if (index == 0)
                    {
                        opts.Title = deck.Metadata.Title;
                    }
                    else
                    {
                        opts.Title += $" + {deck.Metadata.Title}";
                    }

                    index++;
                }
                opts.FailLimit = args.Fail;
                opts.Multiplayer = args.Multiplayer;
                opts.Hardcore = args.Hardcore;
                if (Enum.TryParse(args.Effect, out QuizRenderEffect renderEffect))
                    opts.RenderEffect = renderEffect;

                success = true;
            })
            .WithNotParsed(async errs =>
            {
                await _feedbackService.SendContextualAsync($":no_entry_sign: **Error:** Couldn't parse quiz string!", ct: CancellationToken, options: new(MessageFlags: MessageFlags.Ephemeral));
            });

        if (!success)
        {
            return Result.FromSuccess();
        }

        _quizManager.RegisterQuiz(opts, channelID, guildID, quizCommand);
        _quizManager.Sessions[channelID].AddParticipant(userID);
        
        await _feedbackService.SendContextualAsync($":white_check_mark: **Success:** Starting quiz ``{quizCommand}`` in 5 seconds!", ct: CancellationToken);
        await Task.Delay(5000);

        _quizManager.DoQuiz(_quizManager.Sessions[channelID]);
        return Result.FromSuccess();
    }

    [Command("decks")]
    [Description("Show a list of available quiz decks")]
    [Ephemeral]
    public async Task<IResult> ListDecksAsync()
    {
        StringBuilder logs = new();
        logs.AppendLine("```asciidoc");
        foreach (QuizDeck result in _quizManager.Decks)
        {
            logs.AppendLine($"{result.Metadata.Id} :: {result.Metadata.Title} | {result.Metadata.Description} ");
        }
        logs.AppendLine("```");

        return (Result)await _feedbackService.SendContextualAsync(logs.ToString(), ct: CancellationToken);
    }
}

internal class QuizCommandArgs
{
    [CommandLine.Option('d', "decks", Required = true, HelpText = "Specify a deck to use in this quiz.")]
    public IEnumerable<string> Decks { get; set; }
    [CommandLine.Option('s', "score", Required = false, HelpText = "Score limit to be used for each specified deck. If present, the amount of parameters passed here MUST be equal to the amount of decks specified.")]
    public IEnumerable<string> Scores { get; set; }
    [CommandLine.Option('t', "timeout", Required = false, HelpText = "Timeout to be used for each specified deck, in seconds. If present, the amount of parameters passed here MUST be equal to the amount of decks specified.")]
    public IEnumerable<string> Timeouts { get; set; }
    [CommandLine.Option('f', "fail", Required = false, HelpText = "The amount of fails allowed in this session before the quiz is abandoned.")]
    public int Fail { get; set; }
    [CommandLine.Option('m', "multiplayer", Required = false, HelpText = "Whether or not more participants may join the quiz while it is running.")]
    public bool Multiplayer { get; set; }
    [CommandLine.Option('h', "hardcore", Required = false, HelpText = "Whether or not questions should fail immediately upon a participant submitting an incorrect answer.")]
    public bool Hardcore { get; set; }
    [CommandLine.Option('e', "effect", Required = false, HelpText = "The effect to use to render images in the quiz with.")]
    public string Effect { get; set; }
}