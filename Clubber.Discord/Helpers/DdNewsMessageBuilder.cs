using Clubber.Domain.Extensions;
using Discord;
using System.Text;

namespace Clubber.Discord.Helpers;

public class DdNewsMessageBuilder
{
	private readonly StringBuilder _sb = new();

	public string Build(
		string userName,
		int oldTime,
		int oldRank,
		int newTime,
		int newRank,
		int nth)
	{
		double oldScore = oldTime / 10_000d;
		double newScore = newTime / 10_000d;
		int ranksChanged = oldRank - newRank;

		AppendHeader(userName, oldScore, newScore);
		AppendRankChange(ranksChanged);
		AppendHundredthInfo(oldTime, newTime, nth);

		if (newRank == 1)
		{
			_sb.Append(Format.Bold(" It's a new WR! 👑 🎉"));
		}

		return _sb.ToString();
	}

	private void AppendHeader(string userName, double oldScore, double newScore)
	{
		_sb.Clear()
			.Append("Congratulations to ")
			.Append(userName)
			.Append(" for getting a new PB of ")
			.Append($"{newScore:0.0000}")
			.Append(" seconds! They beat their old PB of ")
			.Append($"{oldScore:0.0000}")
			.Append("s (**+")
			.Append($"{newScore - oldScore:0.0000}")
			.Append("s**), ");
	}

	private void AppendRankChange(int ranksChanged)
	{
		_sb.Append(ranksChanged > 0 ? "gaining " : ranksChanged == 0 ? "but didn't change" : "but lost ")
			.Append(ranksChanged == 0 ? "" : Math.Abs(ranksChanged))
			.Append(Math.Abs(ranksChanged) is 1 or 0 ? " rank." : " ranks.");
	}

	private void AppendHundredthInfo(int oldTime, int newTime, int nth)
	{
		int oldHundredth = oldTime / 1_000_000;
		int newHundredth = newTime / 1_000_000;
		if (newHundredth > oldHundredth)
		{
			_sb.Append(" They are the ")
				.Append(nth)
				.Append(nth.OrdinalNumeral());

			if (oldHundredth < 10 && newHundredth == 10)
				_sb.Append(" player to unlock the leviathan dagger!");
			else
				_sb.Append($" {newHundredth * 100} player!");
		}
	}
}
