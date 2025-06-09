using Xunit.Abstractions;

namespace Clubber.UnitTests.HelpersTests.TestCaseModels;

public sealed record RoleChangeTestCase : IXunitSerializable
{
	public ulong[] UserRoles { get; set; } = [];
	public ulong[] AllPossibleRoles { get; set; } = [];
	public ulong[] RolesToKeep { get; set; } = [];
	public ulong[] ExpectedRolesToAdd { get; set; } = [];
	public ulong[] ExpectedRolesToRemove { get; set; } = [];

	// ReSharper disable once UnusedMember.Global
	public RoleChangeTestCase() { }

	public RoleChangeTestCase(
		ulong[] userRoles,
		ulong[] allPossibleRoles,
		ulong[] rolesToKeep,
		ulong[] expectedRolesToAdd,
		ulong[] expectedRolesToRemove)
	{
		UserRoles = userRoles;
		AllPossibleRoles = allPossibleRoles;
		RolesToKeep = rolesToKeep;
		ExpectedRolesToAdd = expectedRolesToAdd;
		ExpectedRolesToRemove = expectedRolesToRemove;
	}

	public void Deserialize(IXunitSerializationInfo info)
	{
		UserRoles = info.GetValue<ulong[]>(nameof(UserRoles));
		AllPossibleRoles = info.GetValue<ulong[]>(nameof(AllPossibleRoles));
		RolesToKeep = info.GetValue<ulong[]>(nameof(RolesToKeep));
		ExpectedRolesToAdd = info.GetValue<ulong[]>(nameof(ExpectedRolesToAdd));
		ExpectedRolesToRemove = info.GetValue<ulong[]>(nameof(ExpectedRolesToRemove));
	}

	public void Serialize(IXunitSerializationInfo info)
	{
		info.AddValue(nameof(UserRoles), UserRoles);
		info.AddValue(nameof(AllPossibleRoles), AllPossibleRoles);
		info.AddValue(nameof(RolesToKeep), RolesToKeep);
		info.AddValue(nameof(ExpectedRolesToAdd), ExpectedRolesToAdd);
		info.AddValue(nameof(ExpectedRolesToRemove), ExpectedRolesToRemove);
	}
}
