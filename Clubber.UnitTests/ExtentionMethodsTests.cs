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
		Assert.Equal(number.OrdinalIndicator(), expectedOrdinalIndicator);
	}
}
