namespace Clubber.Domain.Models;

public class Result
{
	protected Result(bool isSuccess, string errorMsg)
	{
		if (isSuccess && !string.IsNullOrEmpty(errorMsg))
			throw new InvalidOperationException();

		if (!isSuccess && string.IsNullOrEmpty(errorMsg))
			throw new InvalidOperationException();

		IsSuccess = isSuccess;
		ErrorMsg = errorMsg;
	}

	public bool IsSuccess { get; }
	public string ErrorMsg { get; }
	public bool IsFailure => !IsSuccess;

	public static Result Failure(string message) => new(false, message);

	public static Result<T?> Failure<T>(string message) => new(default, false, message);

	public static Result Success() => new(true, string.Empty);

	public static Result<T> Success<T>(T value) => new(value, true, string.Empty);
}

public class Result<T> : Result
{
	protected internal Result(T value, bool isSuccess, string errorMsg)
		: base(isSuccess, errorMsg)
	{
		Value = value;
	}

	public T Value { get; }
}
