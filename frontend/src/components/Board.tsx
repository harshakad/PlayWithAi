import React from 'react';
import { GamePiece } from './GamePiece';
import { useBoard } from './useBoard';
import './Board.css';
import { RoomDetails } from '../App';

export interface MoveData {
  sourceRow: number;
  sourceCol: number;
  targetRow: number;
  targetCol: number;
  id: number;
}

export interface BoardProps {
  roomDetails: RoomDetails;
  pendingMove?: MoveData | null;
  onMoveFinished?: (sourceRow: number, sourceCol: number, targetRow: number, targetCol: number) => void;
  playingSide?: string;
  onEndTurn?: () => void;
  canEndTurn?: boolean;
  generateInitialBoard: () => (GamePiece | null)[][];
}

const Board: React.FC<BoardProps> = ({
  pendingMove,
  onMoveFinished,
  playingSide: allowedSide,
  onEndTurn,
  canEndTurn,
  generateInitialBoard,
}) => {
  const { board, handleDragStart, handleDrop, handleDragOver } = useBoard(
    generateInitialBoard,
    pendingMove,
    onMoveFinished,
    allowedSide
  );

  const renderBoard = () => {
    if (!board) return null;
    const cells = [];
    for (let row = 0; row < 8; row++) {
      for (let col = 0; col < 8; col++) {
        const isDark = (row + col) % 2 === 1;
        const className = `cell ${isDark ? 'cell-dark' : 'cell-light'}`;

        const pieceData = board[row][col];
        let content = null;
        if (pieceData && typeof pieceData.renderContent === 'function') {
          const canDrag = !allowedSide || pieceData.color === allowedSide;
          content = pieceData.renderContent(row, col, handleDragStart, canDrag);
        }

        cells.push(
          <div
            key={`${row}-${col}`}
            className={className}
            onDragOver={handleDragOver}
            onDrop={(e) => handleDrop(e, row, col)}
          >
            {content}
          </div>
        );
      }
    }
    return cells;
  };

  return (
    <>
      <div className="board-grid">
        {renderBoard()}
      </div>
    </>
  );
};

export default Board;
