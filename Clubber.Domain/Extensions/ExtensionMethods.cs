﻿namespace Clubber.Domain.Extensions;

public static class ExtensionMethods
{
	public static string OrdinalNumeral(this int number) => (number % 100, number % 10) switch
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

	public static int DigitCount(this int n)
	{
		if (n == 0)
		{
			return 1;
		}

		int count = 0;
		while (n != 0)
		{
			count++;
			n /= 10;
		}

		return count;
	}

	public static int FindFirstInt(this string? input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return -1;
		}

		string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		foreach (string part in parts)
		{
			if (int.TryParse(part, out int result))
			{
				return result;
			}
		}

		return -1;
	}
}
