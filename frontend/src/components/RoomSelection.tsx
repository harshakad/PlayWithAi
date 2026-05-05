import React, { useState } from 'react';
import './RoomSelection.css';
import { GameType, RoomDetails, PlayerSide, UserRole } from '../App';
import { api } from '../services/api';

interface RoomSelectionProps {
  gameType: GameType;
  onBack: () => void;
  onJoinRoom: (details: RoomDetails) => void;
}

const RoomSelection: React.FC<RoomSelectionProps> = ({ gameType, onBack, onJoinRoom }) => {
  const [isNewRoom, setIsNewRoom] = useState(true);
  const [roomId, setRoomId] = useState('');
  const [userName, setUserName] = useState('');
  const [role, setRole] = useState<UserRole>('player');
  const [side, setSide] = useState<PlayerSide | ''>('');
  const [loading, setLoading] = useState(false);
  const [playAgainstAi, setPlayAgainstAi] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    if (!isNewRoom && !roomId) return;
    if (!userName) return;
    if (role === 'player' && !side) return;

    setLoading(true);
    try {
      let finalRoomId = roomId;

      if (isNewRoom) {
        const room = await api.createRoom(gameType === 'chess' ? 'Chess' : 'Checkers', `${userName}'s Room`, playAgainstAi);
        finalRoomId = room.id;
      }

      if (role === 'player') {
        const sideParam = side === 'first' ? 'First' : 'Second';
        await api.joinRoom(finalRoomId, userName, sideParam);
      }

      onJoinRoom({
        roomId: finalRoomId,
        userName,
        role,
        side: role === 'player' ? side as PlayerSide : undefined,
      });
    } catch (err: any) {
      setError(err.message || 'Something went wrong');
    } finally {
      setLoading(false);
    }
  };

  const getSides = () => {
    if (gameType === 'chess') return [{ value: 'first', label: 'White (First)' }, { value: 'second', label: 'Black (Second)' }];
    if (gameType === 'checkers') return [{ value: 'first', label: 'Black (First)' }, { value: 'second', label: 'Red (Second)' }];
    return [];
  };

  return (
    <div className="room-setup-container">
      <div className="glass-panel">
        <button className="back-button" onClick={onBack}>← Back</button>
        <h2 className="setup-title">{isNewRoom ? 'Create' : 'Join'} a {gameType === 'chess' ? 'Chess' : 'Checkers'} Game</h2>

        <div className="tab-group">
          <button className={isNewRoom ? 'active' : ''} onClick={() => setIsNewRoom(true)}>New Room</button>
          <button className={!isNewRoom ? 'active' : ''} onClick={() => setIsNewRoom(false)}>Join Existing</button>
        </div>

        {error && <div className="error-message">{error}</div>}

        <form className="setup-form" onSubmit={handleSubmit}>
          <div className="form-group">
            <label>Your Name</label>
            <input type="text" placeholder="Enter your name" value={userName} onChange={e => setUserName(e.target.value)} required />
          </div>

          {!isNewRoom && (
            <div className="form-group">
              <label>Room ID</label>
              <input type="text" placeholder="Paste Room ID here" value={roomId} onChange={e => setRoomId(e.target.value)} required />
            </div>
          )}

          {isNewRoom && (
            <div className="form-group row-group">
              <label>Game Mode</label>
              <div className="checkbox-group">
                <label className="checkbox-label">
                  <input type="checkbox" checked={playAgainstAi} onChange={e => setPlayAgainstAi(e.target.checked)} />
                  Play against AI
                </label>
              </div>
            </div>
          )}

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
          <button type="submit" className="primary-action-button" disabled={loading || (!isNewRoom && !roomId) || !userName || (role === 'player' && !side)}>
            {loading ? 'Processing...' : isNewRoom ? 'Create & Join' : 'Join Room'}
          </button>
        </form>
      </div>
    </div>
  );
};

export default RoomSelection;
