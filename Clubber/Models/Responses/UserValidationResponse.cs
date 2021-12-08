namespace Clubber.Models.Responses
{
	public record struct UserValidationResponse(bool IsError, string? Message);
}
