import React from 'react';
import './Board.css';

export const generateInitialBoard = (gameType) => {
  const board = Array(8).fill(null).map(() => Array(8).fill(null));
  if (gameType === 'chess') {
    const blackPieces = ['♜', '♞', '♝', '♛', '♚', '♝', '♞', '♜'];
    const whitePieces = ['♖', '♘', '♗', '♕', '♔', '♗', '♘', '♖'];
    for (let i = 0; i < 8; i++) {
      board[0][i] = { type: 'chess', piece: blackPieces[i], color: 'black' };
      board[1][i] = { type: 'chess', piece: '♟', color: 'black' };
      board[6][i] = { type: 'chess', piece: '♙', color: 'white' };
      board[7][i] = { type: 'chess', piece: whitePieces[i], color: 'white' };
    }
  } else if (gameType === 'checkers') {
    for (let row = 0; row < 8; row++) {
      for (let col = 0; col < 8; col++) {
        if ((row + col) % 2 === 1) {
          if (row < 3) board[row][col] = { type: 'checkers', color: 'black' };
          else if (row > 4) board[row][col] = { type: 'checkers', color: 'red' };
        }
      }
    }
  }
  return board;
};

const Board = ({ boardState, onMove }) => {
  const handleDragStart = (e, row, col) => {
    e.dataTransfer.setData('sourceRow', row);
    e.dataTransfer.setData('sourceCol', col);

    // Create a drag image on the fly to avoid background ghosting
    // and explicitly freeze its computed dimensions so it won't shrink 
    const target = e.target;
    const computedStyle = window.getComputedStyle(target);
    const rect = target.getBoundingClientRect();
    
    const cloned = target.cloneNode(true);
    cloned.style.position = 'absolute';
    cloned.style.top = '-9999px';
    cloned.style.left = '-9999px';
    
    // Explicitly mirror the exact pixel size the element holds on the grid
    cloned.style.width = `${rect.width}px`;
    cloned.style.height = `${rect.height}px`;
    
    if (boardState[row][col].type === 'checkers') {
      cloned.style.boxShadow = 'inset 0 0 10px rgba(0,0,0,0.5)';
      cloned.style.borderRadius = '50%';
    } else {
      // Fix for chess text getting squashed
      cloned.style.fontSize = computedStyle.fontSize;
      cloned.style.lineHeight = computedStyle.lineHeight;
      cloned.style.display = 'flex'; 
      cloned.style.alignItems = 'center';
      cloned.style.justifyContent = 'center';
      cloned.style.background = 'transparent';
      cloned.style.boxShadow = 'none';
      cloned.style.margin = '0';
    }

    document.body.appendChild(cloned);

    // Keep the cursor relative offset exactly where user clicked!
    const offsetX = e.clientX - rect.left;
    const offsetY = e.clientY - rect.top;

    e.dataTransfer.setDragImage(cloned, offsetX, offsetY);

    // Clean up clone after drag starts and hide the original piece
    setTimeout(() => {
      document.body.removeChild(cloned);
      target.style.visibility = 'hidden'; 
    }, 0);
  };

  const handleDrop = (e, targetRow, targetCol) => {
    e.preventDefault();
    const sourceRow = parseInt(e.dataTransfer.getData('sourceRow'), 10);
    const sourceCol = parseInt(e.dataTransfer.getData('sourceCol'), 10);
    if (sourceRow === targetRow && sourceCol === targetCol) return;
    onMove(sourceRow, sourceCol, targetRow, targetCol);
  };

  const handleDragOver = (e) => {
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

        if (pieceData) {
          if (pieceData.type === 'chess') {
            content = (
              <span
                draggable
                onDragStart={(e) => handleDragStart(e, row, col)}
                onDragEnd={(e) => { e.target.style.visibility = 'visible'; }}
                className="piece-chess"
                style={{ cursor: 'grab', color: pieceData.color === 'white' ? '#fff' : '#000', display: 'inline-block' }}
              >
                {pieceData.piece}
              </span>
            );
          } else if (pieceData.type === 'checkers') {
            content = (
              <div
                draggable
                onDragStart={(e) => handleDragStart(e, row, col)}
                onDragEnd={(e) => { e.target.style.visibility = 'visible'; }}
                className={`piece-checkers-${pieceData.color}`}
                style={{ cursor: 'grab' }}
              ></div>
            );
          }
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
