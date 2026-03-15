using Clubber.Discord.Helpers;
using Clubber.Discord.Models;
using Clubber.Discord.Services;
using Clubber.Domain.Configuration;
using Clubber.Domain.Repositories;
using Clubber.Domain.Models;
using Clubber.Tests.IntegrationTests.Infrastructure;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Clubber.Tests.IntegrationTests.Services;

[Collection("Integration Tests")]
public sealed class DatabaseUpdateServiceIntegrationTests : IDisposable
{
	private readonly IntegrationTestFixture _fixture;
	private readonly ScoreRoleService _scoreRoleService;

	public DatabaseUpdateServiceIntegrationTests(IntegrationTestFixture fixture)
	{
		_fixture = fixture;
		IUserRepository userRepository = new UserRepository(_fixture.DbContext);
		IOptions<AppConfig> appConfig = _fixture.ServiceProvider.GetRequiredService<IOptions<AppConfig>>();
		IServiceScopeFactory scopeFactory = _fixture.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

		_scoreRoleService = new ScoreRoleService(appConfig, scopeFactory, _fixture.WebService, userRepository);
		Substitute.For<IDiscordHelper>();
	}

	public void Dispose()
	{
		_fixture.DbContext.DdPlayers.RemoveRange(_fixture.DbContext.DdPlayers);
		_fixture.DbContext.SaveChanges();
	}

	[Fact]
	public async Task GetBulkUserRoleUpdates_WithNonRegisteredUsers_OnlyProcessesRegistered()
	{
		const ulong registeredUserId = 11111;
		const ulong unregisteredUserId = 99999;
		const int lbId = 101;

		await _fixture.SeedUsersAsync(new DdUser(registeredUserId, lbId));

		_fixture.SetupLeaderboardResponse(
			IntegrationTestFixture.CreateLeaderboardEntry(lbId, 600)
		);

		List<IGuildUser> guildUsers =
		[
			IntegrationTestFixture.CreateMockGuildUser(registeredUserId, "Registered", AppConfig.ScoreRoles[500]),
			IntegrationTestFixture.CreateMockGuildUser(unregisteredUserId, "NotRegistered", AppConfig.ScoreRoles[500])
		];

		BulkUserRoleUpdates result = await _scoreRoleService.GetBulkUserRoleUpdates(guildUsers);

		Assert.Single(result.UserRoleUpdates);
		Assert.Equal(registeredUserId, result.UserRoleUpdates.First().User.Id);
	}

	[Fact]
	public async Task GetBulkUserRoleUpdates_WithRankRoles_AppliesBothScoreAndRankRoles()
	{
		const ulong userId = 11111;
		const int lbId = 101;
		ulong scoreRole500 = AppConfig.ScoreRoles[500];
		ulong expectedScoreRole = AppConfig.ScoreRoles[1000];
		ulong expectedRankRole = AppConfig.RankRoles[3];

		await _fixture.SeedUsersAsync(new DdUser(userId, lbId));

		_fixture.SetupLeaderboardResponse(
			IntegrationTestFixture.CreateLeaderboardEntry(lbId, 1000, rank: 2)
		);

		List<IGuildUser> guildUsers =
		[
			IntegrationTestFixture.CreateMockGuildUser(userId, "TopPlayer", scoreRole500)
		];

		BulkUserRoleUpdates result = await _scoreRoleService.GetBulkUserRoleUpdates(guildUsers);

		Assert.Single(result.UserRoleUpdates);
		UserRoleUpdate update = result.UserRoleUpdates.First();

		Assert.Contains(expectedScoreRole, update.RoleChange.RolesToAdd);
		Assert.Contains(expectedRankRole, update.RoleChange.RolesToAdd);
		Assert.Contains(scoreRole500, update.RoleChange.RolesToRemove);
	}

	[Fact]
	public async Task GetBulkUserRoleUpdates_ComplexScenario_MultipleUsersDifferentNeeds()
	{
		const ulong user1Id = 11111;
		const ulong user2Id = 22222;
		const ulong user3Id = 33333;
		const ulong user4Id = 44444;
		const int lbId1 = 101;
		const int lbId2 = 102;
		const int lbId3 = 103;
		const int lbId4 = 104;

		await _fixture.SeedUsersAsync(
			new DdUser(user1Id, lbId1),
			new DdUser(user2Id, lbId2),
			new DdUser(user3Id, lbId3),
			new DdUser(user4Id, lbId4)
		);

		_fixture.SetupWorldRecords(lbId4);

		_fixture.SetupLeaderboardResponse(
			IntegrationTestFixture.CreateLeaderboardEntry(lbId1, 1000, rank: 5),
			IntegrationTestFixture.CreateLeaderboardEntry(lbId2, 500),
			IntegrationTestFixture.CreateLeaderboardEntry(lbId3, 800),
			IntegrationTestFixture.CreateLeaderboardEntry(lbId4, 1200, rank: 3)
		);

		List<IGuildUser> guildUsers =
		[
			IntegrationTestFixture.CreateMockGuildUser(user1Id, "User1", AppConfig.ScoreRoles[900]),
			IntegrationTestFixture.CreateMockGuildUser(user2Id, "User2", AppConfig.ScoreRoles[600]),
			IntegrationTestFixture.CreateMockGuildUser(user3Id, "User3", AppConfig.ScoreRoles[800]),
			IntegrationTestFixture.CreateMockGuildUser(user4Id, "User4", AppConfig.ScoreRoles[1200])
		];

		BulkUserRoleUpdates result = await _scoreRoleService.GetBulkUserRoleUpdates(guildUsers);

		Assert.Equal(3, result.UserRoleUpdates.Count);

		UserRoleUpdate user1Update = result.UserRoleUpdates.First(u => u.User.Id == user1Id);
		Assert.Contains(AppConfig.ScoreRoles[1000], user1Update.RoleChange.RolesToAdd);
		Assert.Contains(AppConfig.RankRoles[10], user1Update.RoleChange.RolesToAdd);

		UserRoleUpdate user2Update = result.UserRoleUpdates.First(u => u.User.Id == user2Id);
		Assert.Contains(AppConfig.ScoreRoles[500], user2Update.RoleChange.RolesToAdd);
		Assert.Contains(AppConfig.ScoreRoles[600], user2Update.RoleChange.RolesToRemove);

		UserRoleUpdate user4Update = result.UserRoleUpdates.First(u => u.User.Id == user4Id);
		Assert.Contains(AppConfig.FormerWrRoleId, user4Update.RoleChange.RolesToAdd);

		Assert.DoesNotContain(result.UserRoleUpdates, u => u.User.Id == user3Id);
	}

	[Fact]
	public async Task RoleChange_AppliesChanges_CorrectlyCalculatesFinalState()
	{
		const ulong userId = 11111;
		const int lbId = 101;
		ulong oldRole = AppConfig.ScoreRoles[500];
		ulong expectedNewRole = AppConfig.ScoreRoles[800];

		await _fixture.SeedUsersAsync(new DdUser(userId, lbId));

		_fixture.SetupLeaderboardResponse(
			IntegrationTestFixture.CreateLeaderboardEntry(lbId, 800)
		);

		IGuildUser user = IntegrationTestFixture.CreateMockGuildUser(userId, "TestUser", oldRole);

		Result<RoleChange> result = await _scoreRoleService.GetRoleChange(user);

		Assert.True(result.IsSuccess);
		Assert.True(result.Value.HasChanges);

		List<ulong> finalRoles = [.. user.RoleIds];
		finalRoles.RemoveAll(r => result.Value.RolesToRemove.Contains(r));
		finalRoles.AddRange(result.Value.RolesToAdd);

		Assert.Contains(expectedNewRole, finalRoles);
		Assert.DoesNotContain(oldRole, finalRoles);
	}
}
