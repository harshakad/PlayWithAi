import { useState } from 'react';
import GameSelection from './components/GameSelection';
import RoomSelection from './components/RoomSelection';
import GameRoom from './components/GameRoom';
import './App.css';

export type GameType = 'chess' | 'checkers';
export type PlayerSide = 'white' | 'black' | 'red';
export type UserRole = 'player' | 'observer';

export interface RoomDetails {
  roomId: string;
  userName: string;
  role: UserRole;
  side?: PlayerSide;
}

function App() {

  const [gameSelectionState, setGameSelectionState] = useState<GameType | null>(null);
  const [roomDetails, setRoomDetails] = useState<RoomDetails | null>(null);

  return (
    <div className="app-container">
      <header className="header">
        <a href="#" className="logo" onClick={(e) => { e.preventDefault(); setGameSelectionState(null); setRoomDetails(null); }}>PlayWithAI</a>
        <div>
          {/* User profile or settings placeholder */}
        </div>
      </header>
      <main className="main-content">
        {!gameSelectionState ? (
          <GameSelection onSelectGame={setGameSelectionState} />
        ) : !roomDetails ? (
           <RoomSelection 
              gameType={gameSelectionState} 
              onBack={() => setGameSelectionState(null)} 
              onJoinRoom={(details) => setRoomDetails(details)}
           />
        ) : (
          <GameRoom 
             gameType={gameSelectionState} 
             roomDetails={roomDetails}
             onBack={() => { setRoomDetails(null); setGameSelectionState(null); }} 
          />
        )}
      </main>
    </div>
  );
}

export default App;
