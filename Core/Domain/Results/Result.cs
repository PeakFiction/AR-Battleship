using System;

namespace ARBattleship.Core.Domain
{
    /// <summary>
    /// Domain result wrapper: either a success with a T value,
    /// or a failure with a string error message.
    /// Accessing Value on a failure or Error on a success.
    /// </summary>
    public class Result<T>
    {
        /// <summary>True when the operation succeeded and Value is available.</summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Error message explaining why the operation failed.
        /// Only valid when IsSuccess is false.
        /// </summary>
        public string? Error { get; }

        /// <summary>
        /// Only valid when IsSuccess is true.
        /// </summary>
        public T? Value { get; }

        /// <summary>Creates a successful result wrapping value.</summary>
        private Result(T value) { IsSuccess = true; Value = value; Error = null; }

        /// <summary>Creates a failed result with the given error message.</summary>
        private Result(string error) { IsSuccess = false; Error = error; Value = default; }

        /// <summary>Factory: creates a success result.</summary>
        public static Result<T> Success(T value) => new(value);

        /// <summary>Factory: creates a failure result with a descriptive error message.</summary>
        public static Result<T> Failure(string error) => new(error);
    }
}
