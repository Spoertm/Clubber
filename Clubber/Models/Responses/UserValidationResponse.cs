namespace Clubber.Models.Responses
{
	public sealed record UserValidationResponse(bool IsError, string? Message);
}
