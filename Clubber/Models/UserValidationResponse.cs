namespace Clubber.Services
{
	public sealed record UserValidationResponse(bool IsError, string? Message);
}
