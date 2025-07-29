using ScottPlot;
using ScottPlot.Plottables;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Nishizono.Database.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Build.Locator;
using Remora.Discord.API.Abstractions.Rest;
using Nishizono.Bot.Gateway.Quiz;
using SkiaSharp;
using ScottPlot.Colormaps;

public static class ImmersionPlotter
{
    public static MemoryStream PlotImmersionLogs(List<ImmersionLog> logs, DateTime since, DateTime until)
    {
        DateTime oldest = logs.Min(e => e.TimeStamp);

        int uniqueDayCount = logs.Select(e => e.TimeStamp)
            .Select(dt => dt.Date) // strip time, keep only the date part
            .Distinct()
            .Count();

        ScottPlot.Plot myPlot = new();

        // Set background colors
        myPlot.FigureBackground.Color = Colors.Black;
        myPlot.DataBackground.Color = Colors.Black;

        // Optional: Style axes and grid for visibility
        myPlot.Axes.Color(Colors.White); // makes axis labels and ticks white
        myPlot.Grid.MajorLineColor = Colors.DimGray; // subtle grid lines

        // DPI scale
        myPlot.ScaleFactor = 2;

        string[] categoryNames = { "Anime", "Anki", "Book", "Manga", "Listening", "Visual Novel", "YouTube" };
        ScottPlot.Color[] categoryColors = { Colors.MediumOrchid, Colors.DeepSkyBlue, Colors.MediumTurquoise, Colors.SpringGreen, Colors.Yellow, Colors.Coral, Colors.Crimson};

        for (int x = 0; x < (until - since).TotalDays; x++)
        {
            double vn = 0, manga = 0, anime = 0, book = 0, listening = 0, youtube = 0, anki = 0;

            foreach (ImmersionLog log in logs.Where(_ => _.TimeStamp.Date == since.Date.AddDays(x)))
            {
                switch (log.MediaType)
                {
                    case MediaType.VisualNovel:
                        vn += log.Duration.TotalMinutes;
                        break;
                    case MediaType.Manga:
                        manga += log.Duration.TotalMinutes;
                        break;
                    case MediaType.Anime:
                        anime += log.Duration.TotalMinutes;
                        break;
                    case MediaType.Book:
                        book += log.Duration.TotalMinutes;
                        break;
                    case MediaType.Listening:
                        listening += log.Duration.TotalMinutes;
                        break;
                    case MediaType.Youtube:
                        youtube += log.Duration.TotalMinutes;
                        break;
                    case MediaType.Anki:
                        anki += log.Duration.TotalMinutes;
                        break;
                }
            }

            double[] values = { anime, anki, book, manga, listening, vn, youtube };

            double nextBarBase = 0;

            for (int i = 0; i < values.Length; i++)
            {
                ScottPlot.Bar bar = new()
                {
                    Value = nextBarBase + values[i],
                    FillColor = categoryColors[i],
                    ValueBase = nextBarBase,
                    Position = x
                };

                myPlot.Add.Bar(bar);

                nextBarBase += values[i];
            }
        }

        // use custom tick labels on the bottom
        ScottPlot.TickGenerators.NumericManual tickGen = new();
        for (int x = 0; x < (until - since).TotalDays; x++)
        {
            tickGen.AddMajor(x, since.AddDays(x).Date.Day.ToString());
        }
        myPlot.Axes.Bottom.TickGenerator = tickGen;

        // display groups in the legend
        for (int i = 0; i < 7; i++)
        {
            LegendItem item = new()
            {
                LabelText = categoryNames[i],
                FillColor = categoryColors[i]
            };
            myPlot.Legend.ManualItems.Add(item);
        }
        myPlot.Legend.Orientation = Orientation.Horizontal;
        myPlot.Legend.BackgroundColor = Colors.Black;
        myPlot.Legend.OutlineColor = Colors.DimGray;
        myPlot.Legend.FontColor = Colors.White;
        myPlot.ShowLegend(Alignment.UpperCenter);

        // tell the plot to autoscale with no padding beneath the bars
        myPlot.Axes.Margins(bottom: 0, top: .3, left: 0.01, right: 0.01);

        return new MemoryStream(myPlot.GetImageBytes(1600, 1200, ImageFormat.Png));
    }
}
