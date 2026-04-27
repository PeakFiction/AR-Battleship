using System;
using ARBattleship.Core.Application.Enums;

namespace ARBattleship.Core.Application.Common {
    public class Result<TValue, TError>
    {
        private readonly TValue? _value;
        private readonly TError? _error;

        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;

        public TValue Value => IsSuccess
                ? _value!
                : throw new InvalidOperationException("Cannot access Value on a failure result.");

        public TError Error => IsFailure
                ? _error!
                : throw new InvalidOperationException("Cannot access Error on a success result.");


        protected Result(bool isSuccess, TValue? value, TError? error)
        {
            IsSuccess = isSuccess;
            _value = value;
            _error = error;
        }

        public static Result<TValue, TError> Success(TValue value) => new(true, value, default);
        public static Result<TValue, TError> Failure(TError error) => new(false, default, error);
    }

    public sealed class GameResult<T> : Result<T, GameErrorCode>
    {
        private GameResult(bool isSuccess, T? value, GameErrorCode error) 
            : base(isSuccess, value, error) { }

        public static new GameResult<T> Success(T value) => 
            new(true, value, GameErrorCode.None);

        public static new GameResult<T> Failure(GameErrorCode error) => 
            new(false, default, error);
    }

    public static class ResultExtensions
    {
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