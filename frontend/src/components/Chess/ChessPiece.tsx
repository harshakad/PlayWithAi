import React from 'react';
import { GamePiece } from '../GamePiece';

export class ChessPiece extends GamePiece {
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
                    color: this.color === 'first' ? '#fff' : '#000',
                    display: 'inline-block',
                    textShadow: this.color === 'first' ? '-1px -1px 0 #000, 1px -1px 0 #000, -1px 1px 0 #000, 1px 1px 0 #000, 0 4px 8px rgba(0,0,0,0.5)' : undefined
                }}
            >
                {this.piece}
            </span>
        );
    }
}
