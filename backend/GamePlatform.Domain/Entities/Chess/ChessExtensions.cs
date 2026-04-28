using GamePlatform.Domain.Enums;
using GamePlatform.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace GamePlatform.Domain.Entities.Chess
{
    public static class ChessExtensions
    {
        public static Board ApplyPromotions(this Board boardAfterMove, Move move, Piece movedPiece)
        {
            // Pawn promotion: if a pawn reaches the far rank, promote to Queen
            if (movedPiece.Type == PieceType.Pawn)
            {
                bool isPromotion = (movedPiece.Color == PieceColor.White && move.To.Row == 0)
                                || (movedPiece.Color == PieceColor.Black && move.To.Row == 7);

                if (isPromotion)
                {
                    boardAfterMove = boardAfterMove.PromotePieceAt(move.To, PieceType.Queen);
                }
            }

            return boardAfterMove;
        }
    }
}