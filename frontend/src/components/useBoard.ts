import React, { useState, useEffect, useCallback, useRef } from 'react';
import { GamePiece } from './GamePiece';
import { MoveData } from './Board';

export function useBoard(
  generateInitialBoard: () => (GamePiece | null)[][],
  pendingMove?: MoveData | null,
  onMoveFinished?: (sourceRow: number, sourceCol: number, targetRow: number, targetCol: number) => Promise<boolean>,
  allowedSide?: string
) {
  const [board, setBoard] = useState<(GamePiece | null)[][]>(generateInitialBoard);
  const lastProcessedMoveId = useRef<number | undefined>(undefined);

  // Apply opponent moves received via pendingMove prop
  useEffect(() => {
    if (pendingMove && pendingMove.id !== lastProcessedMoveId.current) {
      lastProcessedMoveId.current = pendingMove.id;
      const { sourceRow, sourceCol, targetRow, targetCol } = pendingMove;
      setBoard(prev => {
        if (sourceRow === undefined || sourceCol === undefined) return prev;
        if (!prev[sourceRow] || !prev[sourceRow][sourceCol]) return prev;
        const newBoard = prev.map(row => [...row]);
        newBoard[targetRow][targetCol] = newBoard[sourceRow][sourceCol];
        newBoard[sourceRow][sourceCol] = null;
        return newBoard;
      });
    }
  }, [pendingMove]);

  const applyMove = useCallback((sourceRow: number, sourceCol: number, targetRow: number, targetCol: number) => {
    setBoard(prev => {
      const newBoard = prev.map(row => [...row]);
      newBoard[targetRow][targetCol] = newBoard[sourceRow][sourceCol];
      newBoard[sourceRow][sourceCol] = null;
      return newBoard;
    });
  }, []);

  const handleDragStart = useCallback((e: React.DragEvent, row: number, col: number) => {
    e.dataTransfer.setData('sourceRow', row.toString());
    e.dataTransfer.setData('sourceCol', col.toString());

    const piece = board[row][col];
    if (!piece || (allowedSide && piece.color !== allowedSide)) {
      e.preventDefault();
      return;
    }

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
    const offsetX = (e as unknown as React.MouseEvent).clientX - rect.left;
    const offsetY = (e as unknown as React.MouseEvent).clientY - rect.top;

    e.dataTransfer.setDragImage(cloned, offsetX, offsetY);

    setTimeout(() => {
      if (document.body.contains(cloned)) {
        document.body.removeChild(cloned);
      }
      target.style.visibility = 'hidden';
    }, 0);
  }, [board, allowedSide]);

  const handleDrop = useCallback(async (e: React.DragEvent, targetRow: number, targetCol: number) => {
    e.preventDefault();
    const sourceRow = parseInt(e.dataTransfer.getData('sourceRow'), 10);
    const sourceCol = parseInt(e.dataTransfer.getData('sourceCol'), 10);
    if (sourceRow === targetRow && sourceCol === targetCol) return;

    let previousBoard: (GamePiece | null)[][] = [];
    
    // Optimistic update
    setBoard(prev => {
        previousBoard = prev; // Capture the previous state
        const newBoard = prev.map(row => [...row]);
        newBoard[targetRow][targetCol] = newBoard[sourceRow][sourceCol];
        newBoard[sourceRow][sourceCol] = null;
        return newBoard;
    });

    // Call validation
    const success = await onMoveFinished?.(sourceRow, sourceCol, targetRow, targetCol);
    if (success === false) {
      // Revert if failed
      setBoard(previousBoard);
    }
  }, [onMoveFinished]);

  const handleDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault();
  }, []);

  return { board, handleDragStart, handleDrop, handleDragOver };
}
