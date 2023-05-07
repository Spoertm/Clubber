using Clubber.Domain.Models;
using Clubber.Domain.Models.Exceptions;
using Clubber.Domain.Models.Responses;
using Clubber.Domain.Services;
using Discord;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Clubber.Domain.Helpers;

public class UpdateRolesHelper
{
	private static readonly ImmutableDictionary<int, ulong> _scoreRoles = ImmutableDictionary.CreateRange(new KeyValuePair<int, ulong>[]
	{
		new(1250, 980126799075876874),
		new(1240, 980126039055429655),
		new(1230, 903024433315323915),
		new(1220, 903024200049102948),
		new(1210, 903023707121926195),
		new(1200, 626477161825697803),
		new(1190, 860585008394010634),
		new(1180, 728017461911355454),
		new(1170, 860584658373836800),
		new(1160, 626476794878623756),
		new(1150, 860584131368714292),
		new(1140, 626476562128044052),
		new(1130, 860583620761616384),
		new(1120, 525082045614129163),
		new(1100, 402530230109208577),
		new(1075, 525733825934786570),
		new(1050, 399577125180669963),
		new(1025, 525967813551325196),
		new(1000, 399570979610820608),
		new(950, 728017240762482788),
		new(900, 399570895741386765),
		new(800, 399570790506299398),
		new(700, 399570712018288640),
		new(600, 399569864261632001),
		new(500, 399569581561217024),
		new(400, 399569447771439104),
		new(300, 399569332532674562),
		new(200, 399569259182948363),
		new(100, 399569183966363648),
		new(0, 461203024128376832),
	});
	private readonly List<ulong> _uselessRoles;
	private static readonly ImmutableDictionary<int, ulong> _rankRoles = ImmutableDictionary.CreateRange(new KeyValuePair<int, ulong>[]
	{
		new(1, 446688666325090310),
		new(3, 472451008342261820),
		new(10, 556255819323277312),
		new(25, 992793365684949063),
	});
	private readonly IDatabaseHelper _databaseHelper;
	private readonly IWebService _webService;

	public UpdateRolesHelper(IConfiguration config, IDatabaseHelper databaseHelper, IWebService webService)
	{
		_databaseHelper = databaseHelper;
		_webService = webService;

		ulong unregRoleId = config.GetValue<ulong>("UnregisteredRoleId");
		_uselessRoles = new() { unregRoleId, 458375331468935178, 994354086646399066 };
	}

	public async Task<DatabaseUpdateResponse> UpdateRolesAndDb(IReadOnlyCollection<IGuildUser> guildUsers)
	{
		Stopwatch sw = Stopwatch.StartNew();
		(int nonMemberCount, List<UpdateRolesResponse> updateRolesResponses) = await ExecuteRolesAndDbUpdate(guildUsers);
		sw.Stop();

		int updatedUsers = 0;
		List<Embed> embedList = new();
		foreach (UpdateRolesResponse updateResponse in updateRolesResponses.Where(ur => ur.Success))
		{
			embedList.Add(EmbedHelper.UpdateRoles(updateResponse));
			updatedUsers++;
		}

		string message;
		if (updatedUsers > 0)
			message = $"✅ Successfully updated database and {updatedUsers} user(s).\n🕐 Execution took {sw.ElapsedMilliseconds} ms.";
		else
			message = $"No updates needed today.\nExecution took {sw.ElapsedMilliseconds} ms.";

		if (nonMemberCount > 0)
			message += $"\nℹ️ {nonMemberCount} user(s) are registered but aren't in the server.";

		return new(message, embedList.ToArray());
	}

	private async Task<(int NonMemberCount, List<UpdateRolesResponse> UpdateRolesResponses)> ExecuteRolesAndDbUpdate(IReadOnlyCollection<IGuildUser> guildUsers)
	{
		List<DdUser> dbUsers = await _databaseHelper.GetEntireDatabase();
		List<(DdUser DdUser, IGuildUser GuildUser)> registeredUsers = dbUsers.Join(
				inner: guildUsers,
				outerKeySelector: dbu => dbu.DiscordId,
				innerKeySelector: gu => gu.Id,
				resultSelector: (ddUser, guildUser) => (ddUser, guildUser))
			.ToList();

		IEnumerable<uint> lbIdsToRequest = registeredUsers.Select(ru => (uint)ru.DdUser.LeaderboardId);
		List<EntryResponse> lbPlayers = await _webService.GetLbPlayers(lbIdsToRequest);

		IEnumerable<(IGuildUser GuildUser, EntryResponse LbUser)> updatedUsers = registeredUsers.Join(
				inner: lbPlayers,
				outerKeySelector: ru => ru.DdUser.LeaderboardId,
				innerKeySelector: lbp => lbp.Id,
				resultSelector: (ru, lbp) => (ru.GuildUser, lbp))
			.ToArray();

		List<UpdateRolesResponse> responses = new();

		foreach ((IGuildUser guildUser, EntryResponse lbUser) in updatedUsers)
			responses.Add(await ExecuteRoleUpdate(guildUser, lbUser));

		return (dbUsers.Count - registeredUsers.Count, responses);
	}

	public async Task<UpdateRolesResponse> UpdateUserRoles(IGuildUser user)
	{
		try
		{
			int lbId = (await _databaseHelper.GetDdUserBy(user.Id))!.LeaderboardId;
			List<EntryResponse> lbPlayerList = await _webService.GetLbPlayers(new[] { (uint)lbId });

			return await ExecuteRoleUpdate(user, lbPlayerList[0]);
		}
		catch (ClubberException clubberException)
		{
			Log.Error(clubberException, "Error updating user roles");
			throw;
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Error updating user roles");
			throw new ClubberException("Something went wrong. Chupacabra will get on it soon™.", ex);
		}
	}

	private async Task<UpdateRolesResponse> ExecuteRoleUpdate(IGuildUser guildUser, EntryResponse lbUser)
	{
		(ulong scoreRoleToAdd, ulong[] scoreRolesToRemove) = HandleScoreRoles(guildUser.RoleIds, lbUser.Time);
		(ulong topRoleToAdd, ulong[] topRolesToRemove) = HandleTopRoles(guildUser.RoleIds, lbUser.Rank);
		(decimal? secondsAwayFromNextRole, ulong? nextRoleId) = GetSecondsAwayFromNextRoleAndNextRoleId(lbUser.Time);

		if (scoreRoleToAdd == 0 && scoreRolesToRemove.Length == 0 && topRoleToAdd == 0 && topRolesToRemove.Length == 0)
			return new(
				false,
				secondsAwayFromNextRole,
				nextRoleId,
				null, null, null);

		List<ulong> roleIdsToAdd = new(2);
		if (scoreRoleToAdd != 0)
			roleIdsToAdd.Add(scoreRoleToAdd);

		if (topRoleToAdd != 0)
			roleIdsToAdd.Add(topRoleToAdd);

		ulong[] socketRolesToRemove = scoreRolesToRemove.Concat(topRolesToRemove)
			.ToArray();

		if (roleIdsToAdd.Count > 0)
			await guildUser.AddRolesAsync(roleIdsToAdd);

		if (socketRolesToRemove.Length > 0)
			await guildUser.RemoveRolesAsync(socketRolesToRemove);

		return new(
			true,
			secondsAwayFromNextRole,
			nextRoleId,
			guildUser,
			roleIdsToAdd,
			socketRolesToRemove
		);
	}

	public (ulong ScoreRoleToAdd, ulong[] ScoreRolesToRemove) HandleScoreRoles(IReadOnlyCollection<ulong> userRolesIds, int playerTime)
	{
		(_, ulong scoreRoleId) = _scoreRoles.FirstOrDefault(sr => sr.Key <= playerTime / 10000);

		ulong scoreRoleToAdd = 0;
		if (!userRolesIds.Contains(scoreRoleId))
			scoreRoleToAdd = scoreRoleId;

		IEnumerable<ulong> filteredScoreRoles = _scoreRoles.Values.Where(rid => rid != scoreRoleId).Concat(_uselessRoles);
		return (scoreRoleToAdd, userRolesIds.Intersect(filteredScoreRoles).ToArray());
	}

	public (ulong TopRoleToAdd, ulong[] TopRolesToRemove) HandleTopRoles(IReadOnlyCollection<ulong> userRolesIds, int rank)
	{
		KeyValuePair<int, ulong>? rankRole = _rankRoles.FirstOrDefault(rr => rank <= rr.Key);

		ulong topRoleToAdd = 0;
		if (rankRole.Value.Value == 0)
			return new(topRoleToAdd, userRolesIds.Intersect(_rankRoles.Values).ToArray());

		if (!userRolesIds.Contains(rankRole.Value.Value))
			topRoleToAdd = rankRole.Value.Value;

		IEnumerable<ulong> filteredTopRoles = _rankRoles.Values.Where(rid => rid != rankRole.Value.Value);
		return new(topRoleToAdd, userRolesIds.Intersect(filteredTopRoles).ToArray());
	}

	public (decimal? SecondsAwayFromNextRole, ulong? NextRoleId) GetSecondsAwayFromNextRoleAndNextRoleId(int playerTime)
	{
		(int score, _) = _scoreRoles.FirstOrDefault(sr => sr.Key <= playerTime / 10000);

		if (score == _scoreRoles.Keys.Max())
		{
			return (null, null);
		}

		(int nextScore, ulong nextRoleId) = _scoreRoles.Last(sr => sr.Key > playerTime / 10000);
		decimal secondsAwayFromNextRole = nextScore - playerTime / 10000M;

		return (secondsAwayFromNextRole, nextRoleId);
	}
}
