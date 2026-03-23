import { ChessBoard } from './Chess/ChessBoard';
import { CheckersBoard } from './Checkers/CheckersBoard';

export class GameFactory {
  static getBoardComponent(gameType: string) {
    if (gameType === 'chess') return ChessBoard;
    if (gameType === 'checkers') return CheckersBoard;
    return null;
  }
}
