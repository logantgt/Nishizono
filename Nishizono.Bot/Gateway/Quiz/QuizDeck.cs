using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Nishizono.Bot.Gateway.Quiz;

public class QuizDeck
{
    QuizDeckMeta _meta;
    QuizDeckCard[] _cards;
    public QuizDeck(string path)
    {
        _meta = JsonSerializer.Deserialize<QuizDeckMeta>(File.OpenRead(Path.Combine(path, "meta.json")));
        _cards = JsonSerializer.Deserialize<QuizDeckCard[]>(File.OpenRead(Path.Combine(path, "deck.json")));
    }
    /// <summary>
    /// The metadata that was loaded to represent this deck.
    /// </summary>
    [JsonPropertyName("metadata")]
    public QuizDeckMeta Metadata { get => _meta; }
    /// <summary>
    /// The collection of cards in this deck.
    /// </summary>
    [JsonPropertyName("cards")]
    public QuizDeckCard[] Cards { get => _cards; }
}
public class QuizDeckMeta
{
    /// <summary>
    /// Plain-text friendly title of the deck.
    /// </summary>
    [JsonPropertyName("title")]
    public required string Title { get; set; }
    /// <summary>
    /// Plain-text friendly description of the deck.
    /// </summary>
    [JsonPropertyName("description")]
    public required string Description { get; set; }
    /// <summary>
    /// The internal Id of the deck, used to invoke in quiz commands.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }
    /// <summary>
    /// The format of the deck (basic, multi)
    /// </summary>
    [JsonPropertyName("format")]
    public required string Format { get; set; }
    /// <summary>
    /// The amount of cards in the deck.
    /// </summary>
    [JsonPropertyName("size")]
    public required int Size { get; set; }
    /// <summary>
    /// The default amount of time to wait for answers to be given for questions in the deck.
    /// </summary>
    [JsonPropertyName("time")]
    public required int Time { get; set; }
}
public class QuizDeckCard
{
    /// <summary>
    /// The question in the card.
    /// </summary>
    [JsonPropertyName("question")]
    public required string Question { get; set; }
    /// <summary>
    /// A list of potential answers for the question, shown in a multi-choice quiz.
    /// </summary>
    [JsonPropertyName("options")]
    public required string[] Options { get; set; }
    /// <summary>
    /// Additional instructions displayed with the question of the card.
    /// </summary>
    [JsonPropertyName("instructions")]
    public required string Instructions { get; set; }
    /// <summary>
    /// A list of valid answers for the question.
    /// </summary>
    [JsonPropertyName("answers")]
    public required string[] Answers { get; set; }
    /// <summary>
    /// Additional information displayed with the correct answer(s) to the card.
    /// </summary>
    [JsonPropertyName("comment")]
    public required string Comment { get; set; }
    /// <summary>
    /// How the question on the card should be rendered and displayed. (Image, Text)
    /// </summary>
    [JsonPropertyName("render")]
    public required string Render { get; set; }
}
