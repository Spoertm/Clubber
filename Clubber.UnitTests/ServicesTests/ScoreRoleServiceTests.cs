using Clubber.Discord.Services;
using Clubber.Domain.Configuration;
using Clubber.Domain.Helpers;
using Clubber.Domain.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using System.Collections;
using Xunit;

namespace Clubber.UnitTests.ServicesTests;

public class ScoreRoleServiceTests
{
	private readonly ScoreRoleService _sut;

	public ScoreRoleServiceTests()
	{
		IConfigurationRoot configuration = new ConfigurationBuilder()
			.AddJsonFile("appsettings.Testing.json", optional: false)
			.Build();

		AppConfig appConfig = new();
		configuration.Bind(appConfig);

		Mock<IOptionsMonitor<BotConfig>> mockConfig = new();
		mockConfig.Setup(c => c.CurrentValue).Returns(appConfig.BotConfig);

		Mock<IDatabaseHelper> mockDatabaseHelper = new();
		Mock<IWebService> mockWebService = new();

		_sut = new(mockConfig.Object, mockDatabaseHelper.Object, mockWebService.Object);
	}

	[Theory]
	[ClassData(typeof(GetScoreRoleToKeepTestData))]
	public void GetScoreRoleToKeep_ReturnsExpectedRole(int playerTime, int expectedScoreRoleKey)
	{
		(int scoreKey, ulong _) = _sut.GetScoreRoleToKeep(playerTime * 10_000);

		Assert.Equal(expectedScoreRoleKey, scoreKey);
	}

	[Theory]
	[InlineData(1, 1)]
	[InlineData(2, 3)]
	[InlineData(3, 3)]
	[InlineData(5, 10)]
	[InlineData(10, 10)]
	[InlineData(15, 25)]
	[InlineData(25, 25)]
	[InlineData(30, 0)]
	public void GetRankRoleToKeep_ReturnsExpectedRole(int playerRank, int expectedRankKey)
	{
		(int rankKey, ulong _) = _sut.GetRankRoleToKeep(playerRank);

		Assert.Equal(expectedRankKey, rankKey);
	}

	private class GetScoreRoleToKeepTestData : IEnumerable<object[]>
	{
		private readonly AppConfig _appConfig;

		public GetScoreRoleToKeepTestData()
		{
			IConfigurationRoot configuration = new ConfigurationBuilder()
				.AddJsonFile("appsettings.Testing.json", optional: false)
				.Build();

			AppConfig appConfig = new();
			configuration.Bind(appConfig);

			_appConfig = appConfig;
		}

		public IEnumerator<object[]> GetEnumerator()
		{
			yield return [0, 0];
			yield return [1000, 1000];
			yield return [12, 0];
			yield return [552, 500];
			yield return [1500, _appConfig.BotConfig.ScoreRoles.MaxBy(sr => sr.Key).Key];
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
