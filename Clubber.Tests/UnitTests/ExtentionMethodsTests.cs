using Clubber.Domain.Extensions;
using Xunit;

namespace Clubber.Tests.UnitTests;

public sealed class ExtentionMethodsTests
{
	[Theory]
	[InlineData(1, "st")]
	[InlineData(2, "nd")]
	[InlineData(3, "rd")]
	[InlineData(4, "th")]
	[InlineData(11, "th")]
	[InlineData(12, "th")]
	[InlineData(13, "th")]
	public void OrdinalIndicator_GetsNumber_ReturnsCorrectOrdinalIndicator(int number, string expectedOrdinalIndicator)
	{
		Assert.Equal(number.OrdinalNumeral(), expectedOrdinalIndicator);
	}

	[Theory]
	[InlineData("hello", 10, "hello")]
	[InlineData("hello world", 5, "hell…")]
	[InlineData("test", 4, "test")]
	[InlineData("", 5, "")]
	[InlineData("a", 1, "a")]
	[InlineData("ab", 1, "…")]
	public void Truncate_String_ReturnsExpectedResult(string value, int maxChars, string expected)
	{
		string result = value.Truncate(maxChars);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void Truncate_NullString_ReturnsNull()
	{
		string? value = null;

		string? result = value?.Truncate(10);

		Assert.Null(result);
	}

	[Theory]
	[InlineData(0, 1)]
	[InlineData(1, 1)]
	[InlineData(9, 1)]
	[InlineData(10, 2)]
	[InlineData(99, 2)]
	[InlineData(100, 3)]
	[InlineData(999, 3)]
	[InlineData(1000, 4)]
	[InlineData(-1, 1)]
	[InlineData(-9, 1)]
	[InlineData(-10, 2)]
	[InlineData(-99, 2)]
	[InlineData(-100, 3)]
	[InlineData(int.MaxValue, 10)]
	[InlineData(int.MinValue, 10)]
	public void DigitCount_Integer_ReturnsExpectedCount(int number, int expectedCount)
	{
		int result = number.DigitCount();

		Assert.Equal(expectedCount, result);
	}

	[Theory]
	[InlineData("abc 123 def 456", 123)]
	[InlineData("abc", -1)]
	[InlineData("123abc", -1)]
	[InlineData("-50 abc", -50)]
	[InlineData("", -1)]
	[InlineData("3.14", -1)]
	[InlineData("no numbers here", -1)]
	public void FindFirstInt_ReturnsExpectedResult(string input, int expected)
	{
		int result = input.FindFirstInt();

		Assert.Equal(expected, result);
	}

	[Fact]
	public void FindFirstInt_NullInput_ReturnsMinusOne()
	{
		string? input = null;

		int result = input.FindFirstInt();

		Assert.Equal(-1, result);
	}
}
