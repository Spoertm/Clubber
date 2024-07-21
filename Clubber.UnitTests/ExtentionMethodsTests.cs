using Clubber.Domain.Extensions;
using Xunit;

namespace Clubber.UnitTests;

public class ExtentionMethodsTests
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
