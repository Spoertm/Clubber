using System.Runtime.Serialization;

namespace Clubber.Domain.Models.Exceptions;

[Serializable]
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

	protected ConfigurationMissingException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
