using Clubber.Domain.Extensions;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Globalization;

namespace Clubber.Domain.Helpers;

public sealed class LeaderboardImageGenerator
{
	private const int _imageWidth = 1100;
	private const int _imageHeight = 84;
	private const int _paddingHorizontal = 20;

	private const int _fontSize = 50;
	private const int _textOriginY = _imageHeight / 2 - _fontSize / 2;

	private readonly Font _goetheBoldFont;

	public LeaderboardImageGenerator()
	{
		FontCollection collection = new();
		string fontPath = Path.Combine(AppContext.BaseDirectory, "Data", "GoetheBold.ttf");
		FontFamily family = collection.Add(fontPath, CultureInfo.InvariantCulture);
		_goetheBoldFont = family.CreateFont(_fontSize, FontStyle.Bold);
	}

	public MemoryStream CreateImage(int rank, string username, int time, string? playerCountryCode)
	{
		using Image<Rgba32> image = new(_imageWidth, _imageHeight);
		image.Mutate(ctx => ctx.BackgroundColor(Color.Black));

		image.Mutate(ctx => ctx.DrawText(rank.ToString(), _goetheBoldFont, Color.White, new Point(_paddingHorizontal, _textOriginY)));

		int rankEnd = _paddingHorizontal + rank.DigitCount() * 20;

		int flagPos = rankEnd + _paddingHorizontal;
		string? flagPath = GetPathToFlagPng(playerCountryCode);
		if (flagPath != null)
		{
			Image flag = Image.Load(flagPath);
			image.Mutate(ctx => ctx.DrawImage(flag, new Point(flagPos, 8), 1));
		}

		image.Mutate(ctx => ctx.DrawText(username, _goetheBoldFont, Color.Red, new Point(flagPos + 80, _textOriginY)));
		image.Mutate(ctx => ctx.DrawText((time / 10_000d).ToString("0.0000"), _goetheBoldFont, Color.Red, new Point(890, _textOriginY)));

		MemoryStream stream = new();
		image.SaveAsPng(stream);
		return stream;
	}

	private static string? GetPathToFlagPng(string? playerCountryCode)
	{
		if (string.IsNullOrEmpty(playerCountryCode))
			return null;

		string baseFlagPath = Path.Combine(AppContext.BaseDirectory, "Data", "Flags");
		string flagPath = Path.Combine(baseFlagPath, $"{playerCountryCode}.png");
		if (File.Exists(flagPath))
			return flagPath;

		return null;
	}
}
