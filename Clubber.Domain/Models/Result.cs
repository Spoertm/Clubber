using System.Runtime.CompilerServices;

namespace Clubber.Domain.Models;

/// <summary>Outcome of an operation that either succeeds or fails with a message.</summary>
[Union]
public readonly struct Result : IUnion
{
    private readonly object? _value;

    /// <summary>Initializes a new instance of the <see cref="Result"/> struct holding the <see cref="Success"/> case.</summary>
    /// <param name="value">The <see cref="Success"/> case.</param>
    public Result(Success value) => _value = value;

    /// <summary>Initializes a new instance of the <see cref="Result"/> struct holding the <see cref="Failure"/> case.</summary>
    /// <param name="value">The <see cref="Failure"/> case.</param>
    public Result(Failure value) => _value = value;

    /// <summary>
    /// Gets the case this result holds. An uninitialized result is reported as <see cref="ResultDefaults.Uninitialized"/>
    /// so that <see langword="default"/> can't be mistaken for a success.
    /// </summary>
    public object Value => _value ?? ResultDefaults.Uninitialized;

    public static Result Success() => new Success();

    public static Result<T> Success<T>(T value) => new Success<T>(value);

    public static Result Failure(string message) => new Failure(message);

    public static Result<T> Failure<T>(string message) => new Failure(message);
}

/// <summary>Outcome of an operation that either yields a value or fails with a message.</summary>
/// <typeparam name="T">The type of the value carried on success.</typeparam>
[Union]
public readonly struct Result<T> : IUnion
{
    private readonly object? _value;

    /// <summary>Initializes a new instance of the <see cref="Result{T}"/> struct holding the <see cref="Success{T}"/> case.</summary>
    /// <param name="value">The <see cref="Success{T}"/> case.</param>
    public Result(Success<T> value) => _value = value;

    /// <summary>Initializes a new instance of the <see cref="Result{T}"/> struct holding the <see cref="Failure"/> case.</summary>
    /// <param name="value">The <see cref="Failure"/> case.</param>
    public Result(Failure value) => _value = value;

    /// <summary>
    /// Gets the case this result holds. An uninitialized result is reported as <see cref="ResultDefaults.Uninitialized"/>
    /// so that <see langword="default"/> can't be mistaken for a success.
    /// </summary>
    public object Value => _value ?? ResultDefaults.Uninitialized;
}

/// <summary>Successful outcome that carries no value.</summary>
public sealed record Success;

/// <summary>Successful outcome that carries <see cref="Value"/>.</summary>
/// <typeparam name="T">The type of the carried value.</typeparam>
public sealed record Success<T>(T Value);

/// <summary>Failed outcome that carries a user-facing <see cref="Message"/>.</summary>
public sealed record Failure(string Message);

/// <summary>Cases used when a result is created with <see langword="default"/>.</summary>
internal static class ResultDefaults
{
    /// <summary>Gets the failure reported by an uninitialized result.</summary>
    internal static Failure Uninitialized { get; } = new("Internal error.");
}
