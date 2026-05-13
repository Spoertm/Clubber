using Clubber.Domain.Extensions;
using SkiaSharp;

namespace Clubber.Domain.Helpers;

public sealed class LeaderboardImageGenerator : IDisposable
{
    private const int ImageWidth = 1100;
    private const int ImageHeight = 84;
    private const int PaddingHorizontal = 20;

    private const int FontSize = 50;
    private const int TextOriginY = (ImageHeight / 2) - (FontSize / 2);

    private static readonly string _assetsBasePath = Path.Combine(AppContext.BaseDirectory, "Assets");
    private static readonly string _fontPath = Path.Combine(_assetsBasePath, "GoetheBold.ttf");
    private static readonly string _flagsBasePath = Path.Combine(_assetsBasePath, "Flags");

    private readonly SKTypeface _typeface;
    private readonly SKFont _font;
    private readonly float _baselineY;

    public LeaderboardImageGenerator()
    {
        _typeface = SKTypeface.FromFile(_fontPath) ?? SKTypeface.Default;
        _font = new SKFont(_typeface, FontSize);

        // Measure ascent to align baseline with the original top-based positioning
        _baselineY = TextOriginY - _font.Metrics.Ascent;
    }

    public void Dispose()
    {
        _font.Dispose();
        if (_typeface != SKTypeface.Default)
        {
            _typeface.Dispose();
        }
    }

    public MemoryStream CreateImage(int rank, string username, int time, string? playerCountryCode)
    {
        using SKBitmap bitmap = new(ImageWidth, ImageHeight);
        using SKCanvas canvas = new(bitmap);
        canvas.Clear(SKColors.Black);

        using SKPaint whitePaint = new() { Color = SKColors.White };
        using SKPaint redPaint = new() { Color = SKColors.Red };

        int rankEnd = PaddingHorizontal + (rank.DigitCount() * 20);
        int flagPos = rankEnd + PaddingHorizontal;

        canvas.DrawText(rank.ToString(), PaddingHorizontal, _baselineY, SKTextAlign.Left, _font, whitePaint);

        string? flagPath = GetPathToFlagPng(playerCountryCode);
        if (flagPath != null)
        {
            using SKBitmap flag = SKBitmap.Decode(flagPath);
            if (flag != null)
            {
                canvas.DrawBitmap(flag, flagPos, 8);
            }
        }

        canvas.DrawText(username, flagPos + 80, _baselineY, SKTextAlign.Left, _font, redPaint);
        canvas.DrawText((time / 10_000d).ToString("0.0000"), 890, _baselineY, SKTextAlign.Left, _font, redPaint);

        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
        return new MemoryStream(data.ToArray());
    }

    private static string? GetPathToFlagPng(string? playerCountryCode)
    {
        if (string.IsNullOrEmpty(playerCountryCode))
        {
            return null;
        }

        string flagPath = Path.Combine(_flagsBasePath, $"{playerCountryCode}.png");
        if (File.Exists(flagPath))
        {
            return flagPath;
        }

        return null;
    }
}
