import React from 'react';

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
