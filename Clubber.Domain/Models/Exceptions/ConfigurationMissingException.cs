﻿namespace Clubber.Domain.Models.Exceptions;

public class ConfigurationMissingException : Exception
{
	public ConfigurationMissingException()
	{
	}

	public ConfigurationMissingException(string message)
		: base(message)
	{
	}

	public ConfigurationMissingException(string? message, Exception innerException)
		: base(message, innerException)
	{
	}
}
