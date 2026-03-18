import { useState } from 'react';
import GameSelection from './components/GameSelection';
import GameRoom from './components/GameRoom';
import './App.css';

export type GameType = 'chess' | 'checkers';

function App() {

  const [gameSelectionState, setGameSelectionState] = useState<GameType | null>(null);

  return (
    <div className="app-container">
      <header className="header">
        <a href="#" className="logo" onClick={(e) => { e.preventDefault(); setGameSelectionState(null); }}>PlayWithAI</a>
        <div>
          {/* User profile or settings placeholder */}
        </div>
      </header>
      <main className="main-content">
        {!gameSelectionState ? (
          <GameSelection onSelectGame={setGameSelectionState} />
        ) : (
          <GameRoom gameType={gameSelectionState} onBack={() => setGameSelectionState(null)} />
        )}
      </main>
    </div>
  );
}

export default App;
