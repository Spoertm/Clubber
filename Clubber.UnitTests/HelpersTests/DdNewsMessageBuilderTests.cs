using Clubber.Discord.Helpers;
using Xunit;

namespace Clubber.UnitTests.HelpersTests;

public sealed class DdNewsMessageBuilderTests
{
	private readonly DdNewsMessageBuilder _sut = new();

	[Fact]
	public void Build_WhenNewRankIsOne_AddsWorldRecordMessage()
	{
		const string userName = "TestName";
		const int oldTime = 10000000;
		const int oldRank = 2;
		const int newTime = 9000000;
		const int newRank = 1;
		const int nth = 5;

		string result = _sut.Build(userName, oldTime, oldRank, newTime, newRank, nth);

		Assert.Contains("WR", result);
	}

	[Fact]
	public void Build_WhenRanksIncreased_AddsRankGainMessage()
	{
		const string userName = "TestName";
		const int oldTime = 10000000;
		const int oldRank = 3;
		const int newTime = 9500000;
		const int newRank = 2;
		const int nth = 5;

		string result = _sut.Build(userName, oldTime, oldRank, newTime, newRank, nth);

		Assert.Contains("gaining 1 rank.", result);
	}

	[Fact]
	public void Build_WhenRanksDecreased_AddsRankLossMessage()
	{
		const string userName = "TestName";
		const int oldTime = 10000000;
		const int oldRank = 1;
		const int newTime = 10500000;
		const int newRank = 2;
		const int nth = 5;

		string result = _sut.Build(userName, oldTime, oldRank, newTime, newRank, nth);

		Assert.Contains("but lost 1 rank.", result);
	}

	[Fact]
	public void Build_WhenRanksUnchanged_AddsNoRankChangeMessage()
	{
		const string userName = "TestName";
		const int oldTime = 10000000;
		const int oldRank = 1;
		const int newTime = 10000000;
		const int newRank = 1;
		const int nth = 5;

		string result = _sut.Build(userName, oldTime, oldRank, newTime, newRank, nth);

		Assert.Contains("but didn't change", result);
	}

	[Fact]
	public void Build_WhenNewHundredthGreaterThanOld_AddsHundredthInfo()
	{
		const string userName = "TestName";
		const int oldTime = 10000000; // 1000s
		const int oldRank = 3;
		const int newTime = 11000000; // 1100s
		const int newRank = 2;
		const int nth = 5;

		string result = _sut.Build(userName, oldTime, oldRank, newTime, newRank, nth);

		Assert.Contains("5th", result);
	}
}
