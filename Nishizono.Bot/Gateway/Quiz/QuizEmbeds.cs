using Nishizono.Bot.Remora;
using Remora.Discord.API.Objects;
using Remora.Discord.Extensions.Embeds;
using Remora.Rest.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nishizono.Bot.Gateway.Quiz;

internal static class QuizEmbeds
{
    /// <summary>
    /// Generate a Scoreboard for the given quiz session.
    /// </summary>
    /// <param name="session">The session to generate a scoreboard for.</param>
    /// <returns></returns>
    private static string Scoreboard(QuizSession session)
    {
        // TODO: better scoreboard?
        string scoreboard = "";
        foreach (var users in session.Participants)
        {
            int userPoints = 0;
            foreach (int points in users.Value.Scores.Values)
            {
                userPoints += points;
            }
            scoreboard += $"<@{users.Key}> {userPoints} points\n";
        }
        return scoreboard;
    }

    public static Embed BuildQuizCardEmbed(QuizSession session)
    {
        Enum.TryParse(session.CurrentDeck.Metadata.Format, out QuizDeckFormat deckType);
        Enum.TryParse(session.CurrentCard.Render, out QuizCardRender cardRender);

        EmbedBuilder b = new();
        b.WithTitle(session.CurrentDeck.Metadata.Title);
        b.WithDescription(session.CurrentCard.Instructions);
        b.WithColour(System.Drawing.Color.Magenta);

        if (cardRender == QuizCardRender.Image)
            b.WithImageUrl("attachment://image.png");
        if (cardRender == QuizCardRender.Text)
            b.AddField(
                cardRender == QuizCardRender.Text ? session.CurrentCard.Question : "",
                deckType == QuizDeckFormat.Multi ? MakeFormattedList(session.CurrentCard.Options) : "");

        return b.Build().Get();
    }

    public static Embed BuildQuizAnswerEmbed(QuizSession session)
    {
        Enum.TryParse(session.CurrentDeck.Metadata.Format, out QuizDeckFormat deckType);
        Enum.TryParse(session.CurrentCard.Render, out QuizCardRender cardRender);
        bool correct = session.CurrentCorrectUser.Value != 0;

        EmbedBuilder b = new();
        b.WithTitle(session.CurrentDeck.Metadata.Title);
        b.WithDescription(correct ? $"<@{session.CurrentCorrectUser.Value}> answered first!" : $"Question skipped!");
        b.WithColour(correct ? System.Drawing.Color.LawnGreen : System.Drawing.Color.Red);

        b.AddField(
            "Correct Answers",
            MakeFormattedList(session.CurrentCard.Answers));

        b.AddField(
            "Scores",
            Scoreboard(session));

        if (session.CurrentCard.Comment.Length > 0)
        {
            b.AddField(
                "Comment",
                session.CurrentCard.Comment);
        }

        if (!correct)
            b.WithFooter("You can skip questions by saying 'skip' or just 's' or '。'.");

        return b.Build().Get();
    }

    public static Embed BuildQuizFinishedEmbed(QuizSession session)
    {
        EmbedBuilder b = new();
        b.WithTitle(session.Title + " Ended!");
        b.WithDescription(session.Winner.Value != 0 ? $"<@{session.Winner.Value}> passed first!" : "There were too many unanswered questions, so I stopped!");
        b.WithColour(session.Winner.Value != 0 ? System.Drawing.Color.LawnGreen : System.Drawing.Color.Red);

        if (session.UnansweredQuestions.Count > 0)
        {
            b.AddField(
                "Unanswered Questions",
                MakeFormattedList(session.UnansweredQuestions.ToArray()));
        }

        b.AddField(
            "Scores",
            Scoreboard(session));

        return b.Build().Get();
    }

    public static string MakeFormattedList(string[] list)
    {
        string s = "";

        for (int i = 0; i < list.Length; i++)
        {
            s += $"**{i + 1}.** {list[i]}\n";
        }

        return s;
    }
}
