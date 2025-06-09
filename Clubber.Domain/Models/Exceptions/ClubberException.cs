namespace Clubber.Domain.Models.Exceptions;

public sealed class ClubberException : Exception
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
}
