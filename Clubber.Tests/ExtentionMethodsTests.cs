using Clubber.Extensions;
using Xunit;

namespace Clubber.Tests
{
	public class ExtentionMethodsTests
	{
		[Theory]
		[InlineData(1, "st")]
		[InlineData(2, "nd")]
		[InlineData(3, "rd")]
		[InlineData(4, "th")]
		[InlineData(128, "th")]
		public void OrdinalIndicatorMethodOutputsCorrectly(int number, string expectedOrdinalIndicator)
		{
			Assert.Equal(number.OrdinalIndicator(), expectedOrdinalIndicator);
		}
	}
}
