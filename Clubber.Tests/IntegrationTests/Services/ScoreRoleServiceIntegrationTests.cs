using Clubber.Discord.Models;
using Clubber.Discord.Services;
using Clubber.Domain.Configuration;
using Clubber.Domain.Helpers;
using Clubber.Domain.Models;
using Clubber.Tests.IntegrationTests.Infrastructure;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Clubber.Tests.IntegrationTests.Services;

[Collection("Integration Tests")]
public sealed class ScoreRoleServiceIntegrationTests : IDisposable
{
	private readonly IntegrationTestFixture _fixture;
	private readonly ScoreRoleService _scoreRoleService;

	public ScoreRoleServiceIntegrationTests(IntegrationTestFixture fixture)
	{
		_fixture = fixture;

		IDatabaseHelper databaseHelper = new DatabaseHelper(_fixture.DbContext);
		IOptions<AppConfig> appConfig = _fixture.ServiceProvider.GetRequiredService<IOptions<AppConfig>>();
		IServiceScopeFactory scopeFactory = _fixture.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

		_scoreRoleService = new ScoreRoleService(appConfig, scopeFactory, _fixture.WebService, databaseHelper);
	}

	public void Dispose()
	{
		_fixture.DbContext.DdPlayers.RemoveRange(_fixture.DbContext.DdPlayers);
		_fixture.DbContext.SaveChanges();
	}

	#region GetBulkUserRoleUpdates Tests

	[Fact]
	public async Task GetBulkUserRoleUpdates_NoRegisteredUsers_ReturnsEmptyUpdates()
	{
		IGuildUser guildUser = IntegrationTestFixture.CreateMockGuildUser(12345, "TestUser");

		BulkUserRoleUpdates result = await _scoreRoleService.GetBulkUserRoleUpdates([guildUser]);

		Assert.Empty(result.UserRoleUpdates);
		Assert.Equal(0, result.NonMemberCount);
	}

	[Fact]
	public async Task GetBulkUserRoleUpdates_UserNeedsScoreRoleUpgrade_ReturnsRoleToAdd()
	{
		const ulong userId = 12345;
		const int lbId = 999;
		ulong currentRoleId = AppConfig.ScoreRoles[500];
		ulong expectedNewRoleId = AppConfig.ScoreRoles[600];

		await _fixture.SeedUsersAsync(new DdUser(userId, lbId));

		_fixture.SetupLeaderboardResponse(
			IntegrationTestFixture.CreateLeaderboardEntry(lbId, 600)
		);

		IGuildUser guildUser = IntegrationTestFixture.CreateMockGuildUser(userId, "TestUser", currentRoleId);

		BulkUserRoleUpdates result = await _scoreRoleService.GetBulkUserRoleUpdates([guildUser]);

		Assert.Single(result.UserRoleUpdates);
		UserRoleUpdate update = result.UserRoleUpdates.First();
		Assert.Contains(expectedNewRoleId, update.RoleChange.RolesToAdd);
		Assert.Contains(currentRoleId, update.RoleChange.RolesToRemove);
	}

	[Fact]
	public async Task GetBulkUserRoleUpdates_UserAlreadyHasCorrectRole_NoChanges()
	{
		const ulong userId = 12345;
		const int lbId = 999;
		ulong correctRoleId = AppConfig.ScoreRoles[600];

		await _fixture.SeedUsersAsync(new DdUser(userId, lbId));

		_fixture.SetupLeaderboardResponse(
			IntegrationTestFixture.CreateLeaderboardEntry(lbId, 600)
		);

		IGuildUser guildUser = IntegrationTestFixture.CreateMockGuildUser(userId, "TestUser", correctRoleId);

		BulkUserRoleUpdates result = await _scoreRoleService.GetBulkUserRoleUpdates([guildUser]);

		Assert.Empty(result.UserRoleUpdates);
	}

	[Fact]
	public async Task GetBulkUserRoleUpdates_UserNeedsRankRole_AddsRankRole()
	{
		const ulong userId = 12345;
		const int lbId = 999;
		ulong scoreRoleId = AppConfig.ScoreRoles[1000];
		ulong expectedRankRoleId = AppConfig.RankRoles[3];

		await _fixture.SeedUsersAsync(new DdUser(userId, lbId));

		_fixture.SetupLeaderboardResponse(
			IntegrationTestFixture.CreateLeaderboardEntry(lbId, 1000, rank: 2)
		);

		IGuildUser guildUser = IntegrationTestFixture.CreateMockGuildUser(userId, "TestUser", scoreRoleId);

		BulkUserRoleUpdates result = await _scoreRoleService.GetBulkUserRoleUpdates([guildUser]);

		Assert.Single(result.UserRoleUpdates);
		UserRoleUpdate update = result.UserRoleUpdates.First();
		Assert.Contains(expectedRankRoleId, update.RoleChange.RolesToAdd);
	}

	[Fact]
	public async Task GetBulkUserRoleUpdates_FormerWRHolder_AddsFormerWRRole()
	{
		const ulong userId = 12345;
		const int lbId = 999;
		ulong scoreRoleId = AppConfig.ScoreRoles[1300];

		await _fixture.SeedUsersAsync(new DdUser(userId, lbId));

		_fixture.SetupWorldRecords(lbId);

		_fixture.SetupLeaderboardResponse(
			IntegrationTestFixture.CreateLeaderboardEntry(lbId, 1300, rank: 2)
		);

		IGuildUser guildUser = IntegrationTestFixture.CreateMockGuildUser(userId, "TestUser", scoreRoleId);

		BulkUserRoleUpdates result = await _scoreRoleService.GetBulkUserRoleUpdates([guildUser]);

		Assert.Single(result.UserRoleUpdates);
		UserRoleUpdate update = result.UserRoleUpdates.First();
		Assert.Contains(AppConfig.FormerWrRoleId, update.RoleChange.RolesToAdd);
	}

	[Fact]
	public async Task GetBulkUserRoleUpdates_CurrentWRHolder_DoesNotAddFormerWRRole()
	{
		const ulong userId = 12345;
		const int lbId = 999;
		ulong scoreRoleId = AppConfig.ScoreRoles[1300];
		ulong top1RoleId = AppConfig.RankRoles[1];

		await _fixture.SeedUsersAsync(new DdUser(userId, lbId));

		_fixture.SetupWorldRecords(lbId);

		_fixture.SetupLeaderboardResponse(
			IntegrationTestFixture.CreateLeaderboardEntry(lbId, 1300, rank: 1)
		);

		IGuildUser guildUser = IntegrationTestFixture.CreateMockGuildUser(userId, "TestUser", scoreRoleId);

		BulkUserRoleUpdates result = await _scoreRoleService.GetBulkUserRoleUpdates([guildUser]);

		Assert.Single(result.UserRoleUpdates);
		UserRoleUpdate update = result.UserRoleUpdates.First();
		Assert.Contains(top1RoleId, update.RoleChange.RolesToAdd);
		Assert.DoesNotContain(AppConfig.FormerWrRoleId, update.RoleChange.RolesToAdd);
	}

	[Fact]
	public async Task GetBulkUserRoleUpdates_MultipleUsers_CalculatesCorrectly()
	{
		const ulong user1Id = 11111;
		const ulong user2Id = 22222;
		const int lbId1 = 101;
		const int lbId2 = 102;

		await _fixture.SeedUsersAsync(
			new DdUser(user1Id, lbId1),
			new DdUser(user2Id, lbId2)
		);

		_fixture.SetupLeaderboardResponse(
			IntegrationTestFixture.CreateLeaderboardEntry(lbId1, 600),
			IntegrationTestFixture.CreateLeaderboardEntry(lbId2, 800)
		);

		IGuildUser guildUser1 = IntegrationTestFixture.CreateMockGuildUser(user1Id, "User1", AppConfig.ScoreRoles[500]);
		IGuildUser guildUser2 = IntegrationTestFixture.CreateMockGuildUser(user2Id, "User2", AppConfig.ScoreRoles[700]);

		BulkUserRoleUpdates result = await _scoreRoleService.GetBulkUserRoleUpdates([guildUser1, guildUser2]);

		Assert.Equal(2, result.UserRoleUpdates.Count);
	}

	[Fact]
	public async Task GetBulkUserRoleUpdates_RegisteredUserNotInGuild_ReturnsNonMemberCount()
	{
		const ulong userInGuildId = 11111;
		const ulong userNotInGuildId = 22222;
		const int lbId1 = 101;
		const int lbId2 = 102;

		await _fixture.SeedUsersAsync(
			new DdUser(userInGuildId, lbId1),
			new DdUser(userNotInGuildId, lbId2)
		);

		_fixture.SetupLeaderboardResponse(
			IntegrationTestFixture.CreateLeaderboardEntry(lbId1, 600)
		);

		IGuildUser guildUser = IntegrationTestFixture.CreateMockGuildUser(userInGuildId, "User1", AppConfig.ScoreRoles[500]);

		BulkUserRoleUpdates result = await _scoreRoleService.GetBulkUserRoleUpdates([guildUser]);

		Assert.Equal(1, result.NonMemberCount);
	}

	[Fact]
	public async Task GetBulkUserRoleUpdates_UserWithMultipleIncorrectRoles_RemovesAllExceptScoreRole()
	{
		const ulong userId = 12345;
		const int lbId = 999;
		ulong correctScoreRole = AppConfig.ScoreRoles[600];
		ulong wrongScoreRole1 = AppConfig.ScoreRoles[500];
		ulong wrongScoreRole2 = AppConfig.ScoreRoles[700];
		const ulong uselessRole = 458375331468935178;

		await _fixture.SeedUsersAsync(new DdUser(userId, lbId));

		_fixture.SetupLeaderboardResponse(
			IntegrationTestFixture.CreateLeaderboardEntry(lbId, 600)
		);

		IGuildUser guildUser = IntegrationTestFixture.CreateMockGuildUser(
			userId, "TestUser",
			wrongScoreRole1, wrongScoreRole2, uselessRole
		);

		BulkUserRoleUpdates result = await _scoreRoleService.GetBulkUserRoleUpdates([guildUser]);

		Assert.Single(result.UserRoleUpdates);
		UserRoleUpdate update = result.UserRoleUpdates.First();
		Assert.Contains(correctScoreRole, update.RoleChange.RolesToAdd);
		Assert.Contains(wrongScoreRole1, update.RoleChange.RolesToRemove);
		Assert.Contains(wrongScoreRole2, update.RoleChange.RolesToRemove);
		Assert.Contains(uselessRole, update.RoleChange.RolesToRemove);
	}

	#endregion

	#region GetRoleChange Tests

	[Fact]
	public async Task GetRoleChange_UnregisteredUser_ReturnsFailure()
	{
		IGuildUser user = IntegrationTestFixture.CreateMockGuildUser(99999, "UnregisteredUser");

		Result<RoleChange> result = await _scoreRoleService.GetRoleChange(user);

		Assert.True(result.IsFailure);
		Assert.Contains("not registered", result.ErrorMsg, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task GetRoleChange_UserNeedsUpgrade_ReturnsSuccessWithChanges()
	{
		const ulong userId = 12345;
		const int lbId = 999;
		ulong currentRoleId = AppConfig.ScoreRoles[700];
		ulong expectedNewRoleId = AppConfig.ScoreRoles[800];

		await _fixture.SeedUsersAsync(new DdUser(userId, lbId));

		_fixture.SetupLeaderboardResponse(
			IntegrationTestFixture.CreateLeaderboardEntry(lbId, 800)
		);

		IGuildUser user = IntegrationTestFixture.CreateMockGuildUser(userId, "TestUser", currentRoleId);

		Result<RoleChange> result = await _scoreRoleService.GetRoleChange(user);

		Assert.True(result.IsSuccess, $"Expected success but got: {result.ErrorMsg}");
		Assert.True(result.Value.HasChanges);
		Assert.Contains(expectedNewRoleId, result.Value.RolesToAdd);
		Assert.Contains(currentRoleId, result.Value.RolesToRemove);
	}

	[Fact]
	public async Task GetRoleChange_TopPlayer_HasNoNextMilestone()
	{
		const ulong userId = 12345;
		const int lbId = 999;
		ulong topRoleId = AppConfig.ScoreRoles.MaxBy(sr => sr.Key).Value;
		ulong top1RankRole = AppConfig.RankRoles[1];

		await _fixture.SeedUsersAsync(new DdUser(userId, lbId));

		_fixture.SetupLeaderboardResponse(
			IntegrationTestFixture.CreateLeaderboardEntry(lbId, 1300, rank: 1)
		);

		IGuildUser user = IntegrationTestFixture.CreateMockGuildUser(userId, "TestUser", topRoleId, top1RankRole);

		Result<RoleChange> result = await _scoreRoleService.GetRoleChange(user);

		Assert.True(result.IsSuccess);
		Assert.False(result.Value.HasChanges);
		Assert.Equal(0, result.Value.SecondsToNextMilestone);
	}

	#endregion
}
