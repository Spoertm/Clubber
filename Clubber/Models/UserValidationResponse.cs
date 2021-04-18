namespace Clubber.Models
{
	public sealed record UserValidationResponse(bool IsError, string? Message);
}
