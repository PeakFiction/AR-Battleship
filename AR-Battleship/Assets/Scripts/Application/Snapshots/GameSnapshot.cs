using System;
using Core.Application.Enums;

namespace Core.Application.Snapshots;

[Serializable]
public sealed record GameSnapshot
{
    public PlayerSnapshot PlayerOne { get; init; }
    public PlayerSnapshot PlayerTwo { get; init; }
    public PlayerId CurrentTurn { get; init; }
    public bool IsGameOver { get; init; }
    public PlayerId? Winner { get; init; }

    public GameSnapshot(
        PlayerSnapshot playerOne,
        PlayerSnapshot playerTwo,
        PlayerId currentTurn,
        bool isGameOver,
        PlayerId? winner)
    {
        PlayerOne = playerOne;
        PlayerTwo = playerTwo;
        CurrentTurn = currentTurn;
        IsGameOver = isGameOver;
        Winner = winner;
    }
}