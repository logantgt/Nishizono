using CommandLine;
using Microsoft.Extensions.Options;
using Remora.Rest.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace Nishizono.Bot.Gateway.Quiz;

internal class QuizSession
{
    Dictionary<Snowflake, QuizParticipant> _participants;
    Snowflake _channelId;
    Snowflake _guildId;
    Snowflake _winner;
    Dictionary<QuizDeck, QuizShuffle> _sessionDecks;
    Queue<Tuple<Snowflake, String>> _responses;
    Random _random;
    QuizDeckCard _currentCard;
    QuizDeck _currentDeck;
    Snowflake _currentCorrectUser;
    QuizRenderEffect _renderEffect;
    int _failLimit;
    List<string> _unansweredQuestions;
    string _quizString;
    string _sessionName;
    bool _finished;
    bool _multiplayer;
    bool _hardcore;
    
    public QuizSession(QuizSessionOptions opts, Snowflake channelId, Snowflake guildId, QuizManager quizManager, string quizString)
    {
        _channelId = channelId;
        _guildId = guildId;
        _finished = false;
        _random = new Random();
        _sessionDecks = opts.SessionDecks;
        _hardcore = opts.Hardcore;
        _multiplayer = opts.Multiplayer;
        _failLimit = opts.FailLimit;
        _sessionName = opts.Title;
        _participants = new();
        _quizString = quizString;
        _unansweredQuestions = new();
        _responses = new();
        _winner = new Snowflake(0);
        _currentCorrectUser = new Snowflake(0);
        _renderEffect = opts.RenderEffect;
    }

    public void AddParticipant(Snowflake userId)
    {
        _participants.Add(userId, new QuizParticipant(this, userId));
    }

    public void AddResponse(Snowflake userId, string response)
    {
        if (!_participants.ContainsKey(userId) && _multiplayer)
        {
            AddParticipant(userId);
        }

        if (_participants.ContainsKey(userId))
        {
            _responses.Enqueue(new Tuple<Snowflake, string>(userId, response));
        }
    }
    public void AddUnansweredQuestion(string question)
    {
        _unansweredQuestions.Add(question);
    }
    public void ClearResponses()
    {
        if(_responses.Count > 0) _responses.Clear();
    }
    public void SetWinner(Snowflake userId)
    {
        _winner = userId;
    }

    public void SetCurrentCorrectUser(Snowflake userId)
    {
        _currentCorrectUser = userId;
    }
    public void Finish()
    {
        _finished = true;
    }

    /// <summary>
    /// Randomly select a card from one of the QuizSession's decks. This will set the QuizSession's
    /// CurrentCard and CurrentDeck properties accordingly.
    /// </summary>
    /// <returns>Boolean indicating whether or not the QuizSession could draw a new card from any deck.</returns>
    public bool GetNextCard()
    {
        var decks = _sessionDecks.Where(_ => _.Value.Continue == true);
        var deck = decks.ElementAt(_random.Next(0, decks.Count())).Key;
        
        if (_sessionDecks.TryGetValue(deck, out var shuffle))
        {
            _currentCard = deck.Cards[shuffle.Next()];
            _currentDeck = deck;
            return true;
        }

        _finished = true;
        return false;
    }
    public Dictionary<Snowflake, QuizParticipant> Participants { get => _participants; }
    public Snowflake ChannelId { get => _channelId; }
    public Snowflake GuildId { get => _guildId; }
    public bool Finished { get => _finished; }
    public QuizDeckCard CurrentCard { get => _currentCard; }
    public QuizDeck CurrentDeck { get => _currentDeck; }
    public string QuizString { get => _quizString; }
    public Queue<Tuple<Snowflake, string>> Responses { get => _responses; }
    public List<string> UnansweredQuestions { get => _unansweredQuestions; }
    public Snowflake Winner { get => _winner; }
    public Snowflake CurrentCorrectUser { get => _currentCorrectUser; }
    public string Title { get => _sessionName; }
    public Dictionary<QuizDeck, QuizShuffle> SessionDecks { get => _sessionDecks; }
    public int FailLimit { get => _failLimit; }
    public bool Multiplayer { get => _multiplayer; }
    public bool Hardcore { get => _hardcore; }
    public QuizRenderEffect RenderEffect { get => _renderEffect; }
}

public class QuizSessionOptions
{
    public QuizSessionOptions()
    {
        SessionDecks = new();
    }
    public string Title { get; set; }
    public int FailLimit { get; set; }
    public bool Multiplayer { get; set; }
    public bool Hardcore { get; set; }
    public QuizRenderEffect RenderEffect { get; set; }
    public Dictionary<QuizDeck, QuizShuffle> SessionDecks { get; set; }
}