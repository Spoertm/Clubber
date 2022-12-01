namespace Clubber.Domain.Extensions;

public static class ExtensionMethods
{
	public static string OrdinalIndicator(this int number) => (number % 100, number % 10) switch
	{
		(11 or 12 or 13, _) => "th",
		(_, 1)              => "st",
		(_, 2)              => "nd",
		(_, 3)              => "rd",
		_                   => "th",
	};

	public static string Truncate(this string value, int maxChars)
	{
		if (string.IsNullOrEmpty(value))
			return value;

		return value.Length <= maxChars ? value : value[..(maxChars - 1)] + "…";
	}
}
