import React from 'react';
import { GamePiece } from '../GamePiece';
import { ChessPiece } from './ChessPiece';
import Board, { BoardProps } from '../Board';

function generateInitialBoard(): (GamePiece | null)[][] {
  const board = Array(8).fill(null).map(() => Array(8).fill(null));
  const blackPieces = ['♜', '♞', '♝', '♛', '♚', '♝', '♞', '♜'];
  const whitePieces = ['♖', '♘', '♗', '♕', '♔', '♗', '♘', '♖'];
  for (let i = 0; i < 8; i++) {
    board[0][i] = new ChessPiece('second', blackPieces[i]);
    board[1][i] = new ChessPiece('second', '♟');
    board[6][i] = new ChessPiece('first', '♙');
    board[7][i] = new ChessPiece('first', whitePieces[i]);
  }
  return board;
}

type ChessBoardProps = Omit<BoardProps, 'generateInitialBoard' | 'renderExtras'>;

const ChessBoard: React.FC<ChessBoardProps> = (props) => {
  return (
    <>
      <Board
        {...props}
        generateInitialBoard={generateInitialBoard}
      />
    </>
  );
};

export { ChessBoard };
