using GamePlatform.Domain.Enums;
using GamePlatform.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace GamePlatform.Domain.Entities.Checkers
{
    internal static class CheckersExtensions
    {
        public static Board CapturePiece(this Board boardAfterMove, Move move, Piece movedPiece)
        {
            int absRow = Math.Abs(move.RowDelta);

            // Capture: remove the jumped piece
            if (absRow == 2)
            {
                int midRow = move.From.Row + move.RowDelta / 2;
                int midCol = move.From.Col + move.ColDelta / 2;
                boardAfterMove = boardAfterMove.RemovePieceAt(new BoardPosition(midRow, midCol));
            }

            return boardAfterMove;
        }

        public static Board ApplyPromotions(this Board boardAfterMove, Move move, Piece movedPiece)
        {
            // Promotion: Man reaching the far row becomes a King
            bool shouldPromote = movedPiece.Type == PieceType.Man &&
                ((movedPiece.Color == PieceColor.Black && move.To.Row == 7) ||
                 (movedPiece.Color == PieceColor.Red && move.To.Row == 0));

            if (shouldPromote)
            {
                boardAfterMove = boardAfterMove.PromotePieceAt(move.To, PieceType.CheckersKing);
            }

            return boardAfterMove;
        }
    }
}