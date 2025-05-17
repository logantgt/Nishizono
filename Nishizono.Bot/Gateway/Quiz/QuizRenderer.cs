using Remora.Discord.API.Abstractions.Rest;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nishizono.Bot.Gateway.Quiz;

internal class QuizRenderer
{
    public FileData RenderToRemoraAttachment(string input, QuizRenderEffect effect) =>
    new("image.png", RenderToStream(input, effect));

    private MemoryStream RenderToStream(string input, QuizRenderEffect effect)
    {
        SKBitmap bmp;
        switch (effect)
        {
            case QuizRenderEffect.antiocr:
                bmp = RenderAntiOcrEffect(input);
                break;
            default:
                bmp = RenderNoEffect(input);
                break;
        }
        var memoryStream = new MemoryStream();
        bmp.Encode(memoryStream, SKEncodedImageFormat.Png, 100);
        memoryStream.Seek(0, SeekOrigin.Begin);
        return memoryStream;
    }
    private SKBitmap RenderNoEffect(string word)
    {
        // Fixed margin of 45pt.
        float margin = 45f;

        // 1. Prepare the main text paint using the provided custom font.
        using var textPaint = new SKPaint
        {
            IsAntialias = true,
            TextSize = 96, // Adjust as needed.
            Typeface = SKTypeface.FromFamilyName("ackaisyo")
        };

        // Measure the main text.
        SKRect mainTextBounds = new SKRect();
        textPaint.MeasureText(word, ref mainTextBounds);
        float mainTextWidth = mainTextBounds.Width;
        float mainTextHeight = mainTextBounds.Height;

        // Calculate canvas dimensions with a fixed 30pt margin on all sides.
        int canvasWidth = (int)(mainTextWidth + 2 * margin);
        int canvasHeight = (int)(mainTextHeight + 1 * margin);

        // Create the bitmap and canvas.
        var bitmap = new SKBitmap(canvasWidth, canvasHeight);

        using (var canvas = new SKCanvas(bitmap))
        {
            // Clear to black background
            canvas.Clear(SKColors.Black);

            // Main text
            using (var mainTypeface = SKTypeface.FromFamilyName("ackaisyo"))
            {
                float textSize = 96;
                var textPaintFill = new SKPaint
                {
                    IsAntialias = true,
                    Typeface = mainTypeface,
                    TextSize = textSize,
                    Style = SKPaintStyle.Fill,
                    Color = SKColors.Magenta
                };

                // Measure the text width so we can center it.
                float textWidth = textPaintFill.MeasureText(word);

                // Compute the text position so that it is centered in the canvas.
                // Get font metrics for proper baseline calculation.
                textPaintFill.GetFontMetrics(out SKFontMetrics mainMetrics);
                float x = (canvasWidth - textWidth) / 2;
                // The baseline is computed so that the vertical center of the text is at height/2.
                float baseline = canvasHeight / 2 - (mainMetrics.Ascent + mainMetrics.Descent) / 2;

                // Draw the text
                canvas.DrawText(word, x, baseline, textPaintFill);
            }
        }

        return bitmap;
    }

    public SKBitmap RenderAntiOcrEffect(string word)
    {
        // Fixed margin of 45pt.
        float margin = 45f;

        // 1. Prepare the main text paint using the provided custom font.
        using var textPaint = new SKPaint
        {
            IsAntialias = true,
            TextSize = 96, // Adjust as needed.
            Typeface = SKTypeface.FromFamilyName("ackaisyo")
        };

        // Measure the main text.
        SKRect mainTextBounds = new SKRect();
        textPaint.MeasureText(word, ref mainTextBounds);
        float mainTextWidth = mainTextBounds.Width;
        float mainTextHeight = mainTextBounds.Height;

        // Calculate canvas dimensions with a fixed 30pt margin on all sides.
        int canvasWidth = (int)(mainTextWidth + 2 * margin);
        int canvasHeight = (int)(mainTextHeight + 1 * margin);

        // Create the bitmap and canvas.
        var bitmap = new SKBitmap(canvasWidth, canvasHeight);

        using (var canvas = new SKCanvas(bitmap))
        {
            // Clear to black background
            canvas.Clear(SKColors.Black);

            // OCR text
            string ocrText = string.Concat(Enumerable.Repeat("OCR ", 8));
            int numRows = (int)Math.Ceiling(canvasHeight / 24.0) - 1;

            // Create a paint for the OCR text.
            // The CSS uses "bold 30px 'DejaVu Sans', sans-serif" with color #555555.
            using (var ocrTypeface = SKTypeface.FromFamilyName("DejaVu Sans", SKFontStyle.Bold))
            {
                var ocrPaint = new SKPaint
                {
                    Color = SKColor.Parse("#555555"),
                    IsAntialias = true,
                    Typeface = ocrTypeface,
                    TextSize = 30,
                };

                // Prepare a paint for the drop shadow.
                // CSS drop shadow: offset (6,6) with blur radius 6 and color #555555.
                var ocrShadowPaint = new SKPaint
                {
                    Color = SKColor.Parse("#555555"),
                    IsAntialias = true,
                    Typeface = ocrTypeface,
                    TextSize = 30,
                    MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 6)
                };

                // We need the font metrics to position the text so that its top is at (i * 28).
                ocrPaint.GetFontMetrics(out SKFontMetrics ocrMetrics);
                for (int i = 0; i < numRows; i++)
                {
                    // In CSS, the absolutely positioned div has its top set to i*28.
                    // Since DrawText positions text using the baseline,
                    // we compute: baseline = top - metrics.Ascent.
                    float top = (i * 28) - 10;
                    float baseline = top - ocrMetrics.Ascent;

                    // Draw the shadow first (offset by 6,6)
                    canvas.DrawText(ocrText, 0 + 6, baseline + 6, ocrShadowPaint);
                    // Draw the OCR text
                    canvas.DrawText(ocrText, 0, baseline, ocrPaint);
                }
            }

            // Main text
            using (var mainTypeface = SKTypeface.FromFamilyName("ackaisyo"))
            {
                float textSize = 96;
                var textPaintFill = new SKPaint
                {
                    IsAntialias = true,
                    Typeface = mainTypeface,
                    TextSize = textSize,
                    Style = SKPaintStyle.Fill,
                };

                // Measure the text width so we can build a gradient that runs exactly
                // over the text’s width (left-to-right, as 90deg means).
                float textWidth = textPaintFill.MeasureText(word);

                // Create a horizontal linear gradient with three stops:
                // red, then blue, then magenta.
                textPaintFill.Shader = SKShader.CreateLinearGradient(
                    new SKPoint(0, 0),
                    new SKPoint(255, 0),
                    new SKColor[] { SKColors.Red, SKColors.DarkBlue, SKColors.DarkMagenta },
                    new float[] { 0, 0.5f, 1 },
                    SKShaderTileMode.Clamp);

                // Compute the text position so that it is centered in the canvas.
                // Get font metrics for proper baseline calculation.
                textPaintFill.GetFontMetrics(out SKFontMetrics mainMetrics);
                float x = (canvasWidth - textWidth) / 2;
                // The baseline is computed so that the vertical center of the text is at height/2.
                float baseline = canvasHeight / 2 - (mainMetrics.Ascent + mainMetrics.Descent) / 2;

                // Create a stroke paint for the outline.
                var textPaintStroke = new SKPaint
                {
                    IsAntialias = true,
                    Typeface = mainTypeface,
                    TextSize = textSize,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 2,
                };

                textPaintStroke.Shader = SKShader.CreateLinearGradient(
                    new SKPoint(0, 0),
                    new SKPoint(255, 0),
                    new SKColor[] { SKColors.Blue, SKColors.DarkOrange, SKColors.Gray },
                    new float[] { 0, 0.5f, 1 },
                    SKShaderTileMode.Clamp);

                // Draw the gradient-filled text first
                canvas.DrawText(word, x, baseline, textPaintFill);
                // Then draw the stroke over it
                canvas.DrawText(word, x, baseline, textPaintStroke);
            }
        }

        return bitmap;
    }
}
public enum QuizRenderEffect
{
    none,
    antiocr
}
