namespace Clubber.Domain.Models.Responses;

public record struct UserValidationResponse(bool IsError, string? Message);
