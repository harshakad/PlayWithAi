import React from 'react';
import { GamePiece } from '../GamePiece';

export class CheckersPiece extends GamePiece {
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
                className={`piece-checkers-${this.color === 'first' ? 'black' : 'red'}`}
                style={{ cursor: canDrag ? 'grab' : 'default' }}
            ></div>
        );
    }
}
