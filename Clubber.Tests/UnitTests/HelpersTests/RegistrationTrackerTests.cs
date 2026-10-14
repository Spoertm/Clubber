using Clubber.Domain.Helpers;
using Xunit;

namespace Clubber.Tests.UnitTests.HelpersTests;

public sealed class RegistrationTrackerTests
{
    private const ulong UserId = 1;

    private readonly RegistrationTracker _sut = new();

    [Fact]
    public void TryFlagUser_UnflaggedUser_ReturnsTrue()
    {
        Assert.True(_sut.TryFlagUser(UserId));
    }

    [Fact]
    public void TryFlagUser_FlaggedUser_ReturnsFalse()
    {
        _sut.TryFlagUser(UserId);

        Assert.False(_sut.TryFlagUser(UserId));
    }

    [Fact]
    public void TryFlagUser_RemindedUser_ReturnsFalse()
    {
        _sut.TryFlagUser(UserId);
        _sut.TryMarkUserReminded(UserId);

        Assert.False(_sut.TryFlagUser(UserId));
    }

    [Fact]
    public void TryFlagUser_AfterUnflag_ReturnsTrue()
    {
        _sut.TryFlagUser(UserId);
        _sut.UnflagUser(UserId);

        Assert.True(_sut.TryFlagUser(UserId));
    }

    [Fact]
    public void TryMarkUserReminded_PendingUser_ReturnsTrueOnlyOnce()
    {
        _sut.TryFlagUser(UserId);

        Assert.True(_sut.TryMarkUserReminded(UserId));
        Assert.False(_sut.TryMarkUserReminded(UserId));
    }

    [Fact]
    public void TryMarkUserReminded_UnflaggedUser_ReturnsFalse()
    {
        Assert.False(_sut.TryMarkUserReminded(UserId));
    }
}
