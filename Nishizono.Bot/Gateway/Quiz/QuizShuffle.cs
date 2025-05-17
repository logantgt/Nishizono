namespace Nishizono.Bot.Gateway.Quiz;

/// <summary>
/// Provides random shuffling for card decks in quiz sessions.
/// </summary>
public class QuizShuffle
{
    private int[] _deck;
    private int _index = 0;
    private int _count = 0;
    private bool _continue;
    public QuizShuffle(int count)
    {
        _count = count;
        _deck = new int[count];
        _continue = true;

        for (int i = 0; i < count - 1; i++)
        {
            _deck[i] = i;
        }

        Random r = new Random();

        for (int i = 0; i < count - 1; i++)
        {
            int pos = r.Next(i, count);
            int temp = _deck[i];
            _deck[i] = _deck[pos];
            _deck[pos] = temp;
        }
    }

    public int Next()
    {
        if(_index <= _count) _index++;
        if(_index == _count) _continue = false;
        return _deck[_index - 1];
    }

    public void Shut()
    {
        _continue = false;
    }

    public bool Continue { get => _continue; }
    public int Limit { get; set; }
}
