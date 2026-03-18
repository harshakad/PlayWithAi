import React from 'react';
import './GameSelection.css';
import { GameType } from '../App';

interface GameSelectionProps {
  onSelectGame: (game: GameType) => void;
}

const GameSelection: React.FC<GameSelectionProps> = ({ onSelectGame }) => {
  return (
    <div className="selection-container">
      <h1 className="selection-title">Choose Your Game</h1>
      <div className="cards">
        <div className="game-card" onClick={() => onSelectGame('chess')}>
          <div className="game-icon">♚</div>
          <h2 className="game-title">Chess</h2>
          <p className="game-desc">A classic strategic board game for two players.</p>
        </div>
        <div className="game-card" onClick={() => onSelectGame('checkers')}>
          <div className="game-icon">🔴</div>
          <h2 className="game-title">Checkers</h2>
          <p className="game-desc">Simple yet challenging tactical gameplay.</p>
        </div>
      </div>
    </div>
  );
};

export default GameSelection;
