using Clubber.Domain.Models;
using Xunit;

namespace Clubber.Tests.UnitTests.ModelsTests;

public sealed class ResultTests
{
    [Fact]
    public void Success_CarriesValueThroughSuccessCase()
    {
        Result<int> result = Result.Success(42);

        Assert.Equal(42, Assert.IsType<Success<int>>(result.Value).Value);
    }

    [Fact]
    public void Failure_CarriesMessageThroughFailureCase()
    {
        Result result = Result.Failure("boom");

        Assert.Equal("boom", Assert.IsType<Failure>(result.Value).Message);
    }

    [Fact]
    public void GenericFailure_CarriesMessageThroughFailureCase()
    {
        Result<int> result = Result.Failure<int>("boom");

        Assert.Equal("boom", Assert.IsType<Failure>(result.Value).Message);
    }

    [Fact]
    public void Patterns_MatchTheHeldCase()
    {
        Result<int> success = Result.Success(1);
        Result<int> failure = Result.Failure<int>("boom");

        Assert.True(success is Success<int> { Value: 1 });
        Assert.True(failure is Failure { Message: "boom" });
        Assert.False(success is Failure);
    }

    [Fact]
    public void DefaultResult_IsReportedAsFailureInsteadOfSuccess()
    {
        Result result = default;

        Assert.Equal("Internal error.", Assert.IsType<Failure>(result.Value).Message);
        Assert.False(result is Success);
    }

    [Fact]
    public void DefaultGenericResult_IsReportedAsFailureInsteadOfSuccess()
    {
        Result<int> result = default;

        Assert.Equal("Internal error.", Assert.IsType<Failure>(result.Value).Message);
        Assert.False(result is Success<int>);
    }
}
