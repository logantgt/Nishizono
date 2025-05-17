using Remora.Rest.Core;

namespace Nishizono.Bot.Gateway.Quiz;

internal class QuizParticipant
{
    private Dictionary<QuizDeck, int> _scores;
    private Snowflake _userId;
    public QuizParticipant(QuizSession session, Snowflake userId)
    {
        _scores = new Dictionary<QuizDeck, int>();
        _userId = userId;

        foreach (var deck in session.SessionDecks)
        {
            _scores.Add(deck.Key, 0);
        }
    }

    public void AddPoint(QuizDeck deck)
    {
        if (!_scores.ContainsKey(deck)) _scores.Add(deck, 0);
        _scores[deck] += 1;
    }
    public int GetPoints(QuizDeck deck)
    {
        return _scores[deck];
    }
    public Dictionary<QuizDeck, int> Scores { get => _scores; }
    public Snowflake UserId { get => _userId; }
}
