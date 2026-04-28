using GamePlatform.Domain.Entities.Checkers;
using GamePlatform.Domain.Entities.Chess;
using GamePlatform.Domain.Enums;
using GamePlatform.Domain.Exceptions;
using GamePlatform.Domain.Rules;
using GamePlatform.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;

namespace GamePlatform.Domain.Entities
{
    public abstract class Game : IGame
    {
        public abstract GameType Type { get; }

        public GameStatus Status { get; protected set; } = GameStatus.WaitingForPlayers;

        protected readonly List<Move> _moveHistory = [];

        public IReadOnlyList<Move> MoveHistory => _moveHistory.AsReadOnly();

        public virtual bool IsJoinable => Status is GameStatus.WaitingForPlayers;

        public virtual void StartGame()
        {
            if (Status != GameStatus.WaitingForPlayers)
                throw new GameDomainException("Game cannot be started. Current status: " + Status);

            Status = GameStatus.InProgress;
        }

        public virtual void EndGame()
        {
            if (Status != GameStatus.InProgress)
                throw new GameDomainException("Game cannot be ended. Current status: " + Status);

            Status = GameStatus.Completed;
        }

        public virtual void AbandonGame()
        {
            if (Status != GameStatus.InProgress)
                throw new GameDomainException("Game cannot be abandoned. Current status: " + Status);

            Status = GameStatus.Abandoned;
        }

        public abstract MoveResult EndTurn();

        public abstract MoveResult MakeMove(Side currentTurn, Move move);
    }
}