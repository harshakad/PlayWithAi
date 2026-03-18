import React, { useState } from 'react';
import './RoomSelection.css';
import { GameType, RoomDetails, PlayerSide, UserRole } from '../App';

interface RoomSelectionProps {
  gameType: GameType;
  onBack: () => void;
  onJoinRoom: (details: RoomDetails) => void;
}

const RoomSelection: React.FC<RoomSelectionProps> = ({ gameType, onBack, onJoinRoom }) => {
  const [roomId, setRoomId] = useState('');
  const [userName, setUserName] = useState('');
  const [role, setRole] = useState<UserRole>('player');
  const [side, setSide] = useState<PlayerSide | ''>('');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!roomId || !userName) return;
    if (role === 'player' && !side) return;

    onJoinRoom({
      roomId,
      userName,
      role,
      side: role === 'player' ? side as PlayerSide : undefined,
    });
  };

  const getSides = () => {
    if (gameType === 'chess') return [{value: 'white', label: 'White'}, {value: 'black', label: 'Black'}];
    if (gameType === 'checkers') return [{value: 'red', label: 'Red'}, {value: 'black', label: 'Black'}];
    return [];
  };

  return (
    <div className="room-setup-container">
      <div className="glass-panel">
        <button className="back-button" onClick={onBack}>← Back</button>
        <h2 className="setup-title">Join a {gameType === 'chess' ? 'Chess' : 'Checkers'} Game</h2>
        <form className="setup-form" onSubmit={handleSubmit}>
          <div className="form-group">
            <label>Name</label>
            <input type="text" placeholder="Enter your name" value={userName} onChange={e => setUserName(e.target.value)} required />
          </div>
          <div className="form-group">
            <label>Room ID</label>
            <input type="text" placeholder="Enter Room ID to join or create new" value={roomId} onChange={e => setRoomId(e.target.value)} required />
          </div>
          <div className="form-group row-group">
            <label>Role</label>
            <div className="radio-group">
              <label>
                <input type="radio" value="player" checked={role === 'player'} onChange={() => setRole('player')} />
                Player
              </label>
              <label>
                <input type="radio" value="observer" checked={role === 'observer'} onChange={() => setRole('observer')} />
                Observer
              </label>
            </div>
          </div>
          {role === 'player' && (
            <div className="form-group row-group">
              <label>Side</label>
              <div className="radio-group">
                {getSides().map(s => (
                  <label key={s.value}>
                    <input type="radio" value={s.value} checked={side === s.value} onChange={() => setSide(s.value as PlayerSide)} required />
                    {s.label}
                  </label>
                ))}
              </div>
            </div>
          )}
          <button type="submit" className="primary-action-button" disabled={!roomId || !userName || (role === 'player' && !side)}>Enter Room</button>
        </form>
      </div>
    </div>
  );
};

export default RoomSelection;
