using System.Runtime.Serialization;

namespace Clubber.Domain.Models.Exceptions;

[Serializable]
public class ClubberException : Exception
{
	public ClubberException()
	{
	}

	public ClubberException(string message)
		: base(message)
	{
	}

	public ClubberException(string? message, Exception innerException)
		: base(message, innerException)
	{
	}

	protected ClubberException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
