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

        /// <summary>
        /// Returns the Forsyth-Edwards Notation (FEN) string for the given board state.
        /// Only the piece placement is generated; other fields must be provided.
        /// </summary>
        public static string ToFen(
            this Board board,
            PieceColor activeColor = PieceColor.White,
            string castling = "KQkq",
            string enPassant = "-",
            int halfmoveClock = 0,
            int fullmoveNumber = 1)
        {
            var sb = new StringBuilder();
            for (int row = 0; row < Board.Size; row++)
            {
                int empty = 0;
                for (int col = 0; col < Board.Size; col++)
                {
                    var piece = board[row, col];
                    if (piece is null)
                    {
                        empty++;
                    }
                    else
                    {
                        if (empty > 0)
                        {
                            sb.Append(empty);
                            empty = 0;
                        }
                        sb.Append(PieceToFenChar(piece));
                    }
                }
                if (empty > 0)
                    sb.Append(empty);
                if (row < Board.Size - 1)
                    sb.Append('/');
            }

            // Active color
            sb.Append(' ');
            sb.Append(activeColor == PieceColor.White ? 'w' : 'b');

            // Castling rights
            sb.Append(' ');
            sb.Append(string.IsNullOrEmpty(castling) ? "-" : castling);

            // En passant
            sb.Append(' ');
            sb.Append(string.IsNullOrEmpty(enPassant) ? "-" : enPassant);

            // Halfmove clock
            sb.Append(' ');
            sb.Append(halfmoveClock);

            // Fullmove number
            sb.Append(' ');
            sb.Append(fullmoveNumber);

            return sb.ToString();
        }

        private static char PieceToFenChar(Piece piece)
        {
            // FEN: uppercase = White, lowercase = Black
            return (piece.Type, piece.Color) switch
            {
                (PieceType.King, PieceColor.White) => 'K',
                (PieceType.Queen, PieceColor.White) => 'Q',
                (PieceType.Rook, PieceColor.White) => 'R',
                (PieceType.Bishop, PieceColor.White) => 'B',
                (PieceType.Knight, PieceColor.White) => 'N',
                (PieceType.Pawn, PieceColor.White) => 'P',
                (PieceType.King, PieceColor.Black) => 'k',
                (PieceType.Queen, PieceColor.Black) => 'q',
                (PieceType.Rook, PieceColor.Black) => 'r',
                (PieceType.Bishop, PieceColor.Black) => 'b',
                (PieceType.Knight, PieceColor.Black) => 'n',
                (PieceType.Pawn, PieceColor.Black) => 'p',
                _ => '?'
            };
        }
    }
}