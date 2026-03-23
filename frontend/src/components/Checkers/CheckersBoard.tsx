import React from 'react';
import { GamePiece } from '../GamePiece';
import { CheckersPiece } from './CheckersPiece';
import Board, { BoardProps } from '../Board';

function generateInitialBoard(): (GamePiece | null)[][] {
  const board = Array(8).fill(null).map(() => Array(8).fill(null));
  for (let row = 0; row < 8; row++) {
    for (let col = 0; col < 8; col++) {
      if ((row + col) % 2 === 1) {
        if (row < 3) board[row][col] = new CheckersPiece('first');
        else if (row > 4) board[row][col] = new CheckersPiece('second');
      }
    }
  }
  return board;
}

type CheckersBoardProps = Omit<BoardProps, 'generateInitialBoard' | 'renderExtras'>;

const CheckersBoard: React.FC<CheckersBoardProps> = (props) => {
  return (
    <>
      <Board
        {...props}
        generateInitialBoard={generateInitialBoard}
      />
      <button
        className="end-turn-button"
        onClick={() => props.onEndTurn?.()}
        disabled={!props.canEndTurn}
      >
        End Turn
      </button>
    </>
  );
};

export { CheckersBoard };
