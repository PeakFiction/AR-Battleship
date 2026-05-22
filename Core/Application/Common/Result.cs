// Application-layer result types.  Unlike the domain's Result<T> (which uses a
// plain string for errors), these use a typed GameErrorCode enum so consumers
// can branch on specific error categories without string parsing.
using System;
using ARBattleship.Core.Application.Enums;

namespace ARBattleship.Core.Application.Common
{
    /// <summary>
    /// Generic discriminated union: either a success with <typeparamref name="TValue"/>
    /// or a failure with <typeparamref name="TError"/>.
    /// Accessing <see cref="Value"/> on failure or <see cref="Error"/> on success
    /// throws <see cref="InvalidOperationException"/>.
    /// </summary>
    public class Result<TValue, TError>
    {
        private readonly TValue? _value;
        private readonly TError? _error;

        /// <summary>True when the operation succeeded.</summary>
        public bool IsSuccess { get; }

        /// <summary>True when the operation failed.</summary>
        public bool IsFailure => !IsSuccess;

        /// <summary>
        /// The success value.
        /// <exception cref="InvalidOperationException">Thrown when accessed on a failure.</exception>
        /// </summary>
        public TValue Value => IsSuccess
            ? _value!
            : throw new InvalidOperationException("Cannot access Value on a failure result.");

        /// <summary>
        /// The error value.
        /// <exception cref="InvalidOperationException">Thrown when accessed on a success.</exception>
        /// </summary>
        public TError Error => IsFailure
            ? _error!
            : throw new InvalidOperationException("Cannot access Error on a success result.");

        /// <summary>Protected constructor; use the static factory methods.</summary>
        protected Result(bool isSuccess, TValue? value, TError? error)
        {
            IsSuccess = isSuccess;
            _value    = value;
            _error    = error;
        }

        /// <summary>Creates a success result wrapping <paramref name="value"/>.</summary>
        public static Result<TValue, TError> Success(TValue value) => new(true, value, default);

        /// <summary>Creates a failure result wrapping <paramref name="error"/>.</summary>
        public static Result<TValue, TError> Failure(TError error) => new(false, default, error);
    }

    /// <summary>
    /// Application-layer result fixed to <see cref="GameErrorCode"/> for errors.
    /// Use this in all service methods that can fail with a game-specific reason.
    /// </summary>
    public sealed class GameResult<T> : Result<T, GameErrorCode>
    {
        private GameResult(bool isSuccess, T? value, GameErrorCode error)
            : base(isSuccess, value, error) { }

        /// <summary>Creates a success result.</summary>
        public static new GameResult<T> Success(T value) =>
            new(true, value, GameErrorCode.None);

        /// <summary>Creates a failure result with the given error code.</summary>
        public static new GameResult<T> Failure(GameErrorCode error) =>
            new(false, default, error);
    }

    /// <summary>
    /// Extension helpers for converting domain-layer <see cref="Domain.Result{T}"/>
    /// objects into application-layer <see cref="GameResult{T}"/> objects.
    /// </summary>
    public static class ResultExtensions
    {
        /// <summary>
        /// Converts a domain Result to a GameResult, mapping failure to the
        /// given <paramref name="errorIfFailed"/> code (default Unknown).
        /// </summary>
        public static GameResult<T> ToApplicationResult<T>(
            this Domain.Result<T> domainResult,
            GameErrorCode errorIfFailed = GameErrorCode.Unknown)
        {
            return domainResult.IsSuccess
                ? GameResult<T>.Success(domainResult.Value!)
                : GameResult<T>.Failure(errorIfFailed);
        }
    }
}
