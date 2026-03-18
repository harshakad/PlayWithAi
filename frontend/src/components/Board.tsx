import React from 'react';
import './Board.css';

export abstract class GamePiece {
  color: string;
  type: string;

  constructor(color: string, type: string) {
    this.color = color;
    this.type = type;
  }
  abstract getCloneStyle(_computedStyle: CSSStyleDeclaration): React.CSSProperties;
  abstract renderContent(_row: number, _col: number, _handleDragStart: (e: React.DragEvent, row: number, col: number) => void, _canDrag: boolean): React.ReactNode;
}

class ChessPiece extends GamePiece {
  piece: string;

  constructor(color: string, piece: string) {
    super(color, 'chess');
    this.piece = piece;
  }
  override getCloneStyle(computedStyle: CSSStyleDeclaration): React.CSSProperties {
    return {
      fontSize: computedStyle.fontSize,
      lineHeight: computedStyle.lineHeight,
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      background: 'transparent',
      boxShadow: 'none',
      margin: '0'
    };
  }
  override renderContent(row: number, col: number, handleDragStart: (e: React.DragEvent, row: number, col: number) => void, canDrag: boolean): React.ReactNode {
    return (
      <span
        draggable
        onDragStart={(e) => handleDragStart(e, row, col)}
        onDragEnd={(e) => { (e.target as HTMLElement).style.visibility = 'visible'; }}
        className="piece-chess"
        style={{
          cursor: canDrag ? 'grab' : 'default',
          color: this.color === 'white' ? '#fff' : '#000',
          display: 'inline-block',
          textShadow: this.color === 'white' ? '-1px -1px 0 #000, 1px -1px 0 #000, -1px 1px 0 #000, 1px 1px 0 #000, 0 4px 8px rgba(0,0,0,0.5)' : undefined
        }}
      >
        {this.piece}
      </span>
    );
  }
}

class CheckersPiece extends GamePiece {
  constructor(color: string) {
    super(color, 'checkers');
  }
  override getCloneStyle(_computedStyle: CSSStyleDeclaration): React.CSSProperties {
    return {
      boxShadow: 'inset 0 0 10px rgba(0,0,0,0.5)',
      borderRadius: '50%'
    };
  }
  override renderContent(row: number, col: number, handleDragStart: (e: React.DragEvent, row: number, col: number) => void, canDrag: boolean): React.ReactNode {
    return (
      <div
        draggable
        onDragStart={(e) => handleDragStart(e, row, col)}
        onDragEnd={(e) => { (e.target as HTMLElement).style.visibility = 'visible'; }}
        className={`piece-checkers-${this.color}`}
        style={{ cursor: canDrag ? 'grab' : 'default' }}
      ></div>
    );
  }
}

const boardGenerators = {
  chess: () => {
    const board = Array(8).fill(null).map(() => Array(8).fill(null));
    const blackPieces = ['♜', '♞', '♝', '♛', '♚', '♝', '♞', '♜'];
    const whitePieces = ['♖', '♘', '♗', '♕', '♔', '♗', '♘', '♖'];
    for (let i = 0; i < 8; i++) {
      board[0][i] = new ChessPiece('black', blackPieces[i]);
      board[1][i] = new ChessPiece('black', '♟');
      board[6][i] = new ChessPiece('white', '♙');
      board[7][i] = new ChessPiece('white', whitePieces[i]);
    }
    return board;
  },
  checkers: () => {
    const board = Array(8).fill(null).map(() => Array(8).fill(null));
    for (let row = 0; row < 8; row++) {
      for (let col = 0; col < 8; col++) {
        if ((row + col) % 2 === 1) {
          if (row < 3) board[row][col] = new CheckersPiece('black');
          else if (row > 4) board[row][col] = new CheckersPiece('red');
        }
      }
    }
    return board;
  }
};

export const generateInitialBoard = (gameType: string): (GamePiece | null)[][] => {
  return boardGenerators[gameType as keyof typeof boardGenerators] ? boardGenerators[gameType as keyof typeof boardGenerators]() : Array(8).fill(null).map(() => Array(8).fill(null));
};

interface BoardProps {
  boardState: (GamePiece | null)[][];
  onMove: (sourceRow: number, sourceCol: number, targetRow: number, targetCol: number) => void;
  allowedSide?: string;
}

const Board: React.FC<BoardProps> = ({ boardState, onMove, allowedSide }) => {
  const handleDragStart = (e: React.DragEvent, row: number, col: number) => {
    e.dataTransfer.setData('sourceRow', row.toString());
    e.dataTransfer.setData('sourceCol', col.toString());

    // Prevent dragging if it's not the user's turn/piece based on role
    const piece = boardState[row][col];
    if (!piece || (allowedSide && piece.color !== allowedSide)) {
      e.preventDefault();
      return;
    }

    // Only create clone and execute styling if drag actually processed 
    const target = e.target as HTMLElement;
    const computedStyle = window.getComputedStyle(target);
    const rect = target.getBoundingClientRect();

    const cloned = target.cloneNode(true) as HTMLElement;
    cloned.style.position = 'absolute';
    cloned.style.top = '-9999px';
    cloned.style.left = '-9999px';
    
    cloned.style.width = `${rect.width}px`;
    cloned.style.height = `${rect.height}px`;

    if (typeof piece.getCloneStyle === 'function') {
      const styles = piece.getCloneStyle(computedStyle);
      Object.assign(cloned.style, styles);
    }

    document.body.appendChild(cloned);

    // Keep the cursor relative offset exactly where user clicked!
    const offsetX = (e as unknown as React.MouseEvent).clientX - rect.left;
    const offsetY = (e as unknown as React.MouseEvent).clientY - rect.top;

    e.dataTransfer.setDragImage(cloned, offsetX, offsetY);

    // Clean up clone after drag starts and hide the original piece
    setTimeout(() => {
      document.body.removeChild(cloned);
      target.style.visibility = 'hidden';
    }, 0);
  };

  const handleDrop = (e: React.DragEvent, targetRow: number, targetCol: number) => {
    e.preventDefault();
    const sourceRow = parseInt(e.dataTransfer.getData('sourceRow'), 10);
    const sourceCol = parseInt(e.dataTransfer.getData('sourceCol'), 10);
    if (sourceRow === targetRow && sourceCol === targetCol) return;
    onMove(sourceRow, sourceCol, targetRow, targetCol);
  };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
  };

  const renderBoard = () => {
    if (!boardState) return null;
    let cells = [];
    for (let row = 0; row < 8; row++) {
      for (let col = 0; col < 8; col++) {
        const isDark = (row + col) % 2 === 1;
        const className = `cell ${isDark ? 'cell-dark' : 'cell-light'}`;

        const pieceData = boardState[row][col];
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
    <div className="board-grid">
      {renderBoard()}
    </div>
  );
};

export default Board;
