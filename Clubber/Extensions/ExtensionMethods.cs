﻿namespace Clubber.Extensions;

public static class ExtensionMethods
{
	public static string OrdinalIndicator(this int number) => (number % 10) switch
	{
		1 => "st",
		2 => "nd",
		3 => "rd",
		_ => "th",
	};

	public static string Truncate(this string value, int maxChars)
	{
		if (string.IsNullOrEmpty(value))
			return value;

		return value.Length <= maxChars ? value : value[..(maxChars - 1)] + "…";
	}
}
