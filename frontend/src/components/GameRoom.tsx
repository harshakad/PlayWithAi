import React, { useState, useEffect } from 'react';
import * as signalR from '@microsoft/signalr';
import { GameFactory } from './GameFactory';
import { MoveData } from './Board';
import { api } from '../services/api';

import './GameRoom.css';
import { GameType, RoomDetails } from '../App';

interface GameRoomProps {
  gameType: GameType;
  roomDetails: RoomDetails;
  onBack: () => void;
}

interface Message {
  id: number;
  sender: 'system' | 'self' | string;
  text: string;
}

const GameRoom: React.FC<GameRoomProps> = ({ gameType, roomDetails, onBack }) => {
  const [messages, setMessages] = useState<Message[]>([
    { id: 1, sender: 'system', text: 'Welcome to the room! Waiting for opponent...' }
  ]);
  const [inputText, setInputText] = useState('');
  const [pendingMove, setPendingMove] = useState<MoveData | null>(null);

  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
  const [currentTurn, setCurrentTurn] = useState<string>('first');
  const [gameOver, setGameOver] = useState<{ isOver: boolean; reason?: string }>({ isOver: false });
  const roomId = roomDetails.roomId;

  const handleGameOver = (isGameOver: boolean, reason?: string) => {
    if (isGameOver) {
      setGameOver({ isOver: true, reason });
      setMessages(prev => [
        ...prev,
        { id: Date.now(), sender: 'system', text: `GAME OVER: ${reason || 'The game has ended.'}` }
      ]);
    }
  };

  // Fetch initial room state
  useEffect(() => {
    const fetchInitialState = async () => {
      try {
        const room = await api.getRoom(roomId);
        const turn = room.currentTurn === 'First' ? 'first' : 'second';
        setCurrentTurn(turn);
      } catch (err) {
        console.error("Failed to fetch initial room state", err);
      }
    };
    fetchInitialState();
  }, [roomId]);

  // Setup SignalR connection
  useEffect(() => {
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl("http://localhost:5039/gamehub")
      .withAutomaticReconnect()
      .build();

    setConnection(newConnection);

    return () => {
      newConnection.stop();
    };
  }, []);

  // Configure SignalR events
  useEffect(() => {
    if (connection) {
      connection.start()
        .then(() => {
          console.log('Connected to SignalR!');
          connection.invoke('JoinRoom', roomId, roomDetails.userName);

          connection.on('UserJoined', (joinedUserName: string) => {
            if (joinedUserName !== roomDetails.userName) {
              setMessages(prev => [...prev, { id: Date.now(), sender: 'system', text: `${joinedUserName} has joined the room.` }]);
            }
          });

          connection.on('ReceiveMessage', (sender: string, message: string) => {
            setMessages(prev => [...prev, { id: Date.now(), sender: sender === roomDetails.userName ? 'self' : sender, text: message }]);
          });

          connection.on('ReceiveMove', (moveData: { sourceRow: number, sourceCol: number, targetRow: number, targetCol: number, isGameOver?: boolean, gameOverReason?: string }) => {
            setPendingMove({ ...moveData, id: Date.now() });
            if (moveData.isGameOver) {
              handleGameOver(true, moveData.gameOverReason);
            }
          });

          connection.on('ReceiveTurnUpdate', (nextTurn: string) => {
            setCurrentTurn(nextTurn);
            setMessages(prev => [...prev, { id: Date.now(), sender: 'system', text: `It is now ${nextTurn}'s turn.` }]);
          });
        })
        .catch(e => console.log('Connection failed: ', e));
    }
  }, [connection, roomId, roomDetails.userName]);

  const handleMoveFinished = async (sourceRow: number, sourceCol: number, targetRow: number, targetCol: number) => {
    if (roomDetails.role !== 'player') return false;

    try {
      const result = await api.makeMove(roomId, roomDetails.userName, { sourceRow, sourceCol, targetRow, targetCol });
      if (result.isGameOver) {
        handleGameOver(true, result.gameOverReason);
      } else if (gameType === 'chess') {
        await endTurn();
      }
      return true;
    } catch (err: any) {
      console.error("Move validation failed", err);
      setMessages(prev => [...prev, { id: Date.now(), sender: 'system', text: `Move failed: ${err.message}` }]);
      return false;
    }
  };

  const endTurn = async () => {
    try {
      const result = await api.endTurn(roomId);
      if (result.isGameOver) {
        handleGameOver(true, result.gameOverReason);
      }
      // The backend will broadcast the turn update via SignalR
    } catch (err: any) {
      console.error("EndTurn failed", err);
      setMessages(prev => [...prev, { id: Date.now(), sender: 'system', text: `End turn failed: ${err.message}` }]);
    }
  };

  const handleSend = (e: React.SyntheticEvent) => {
    e.preventDefault();
    if (!inputText.trim()) return;

    if (connection && connection.state === signalR.HubConnectionState.Connected) {
      connection.invoke('SendMessage', roomId, roomDetails.userName, inputText);
    } else {
      setMessages([...messages, { id: Date.now(), sender: 'self', text: inputText }]);
    }

    setInputText('');
  };

  return (
    <div className="room-container">
      <div className="board-section">
        <button className="back-button" onClick={onBack}>← Back</button>
        {(() => {
          const BoardComponent = GameFactory.getBoardComponent(gameType);
          return BoardComponent ? (
            <BoardComponent
              roomDetails={roomDetails}
              pendingMove={pendingMove}
              onMoveFinished={handleMoveFinished}
              playingSide={roomDetails.role === 'player' && currentTurn === roomDetails.side ? roomDetails.side : 'none'}
              onEndTurn={endTurn}
              canEndTurn={roomDetails.role === 'player' && currentTurn === roomDetails.side && !gameOver.isOver}
            />
          ) : null;
        })()}

        {gameOver.isOver && (
          <div className="game-over-overlay">
            <div className="game-over-content">
              <h2>Game Over</h2>
              <p>{gameOver.reason || 'The game has ended.'}</p>
              <button onClick={onBack}>Back to Lobby</button>
            </div>
          </div>
        )}
      </div>

      <div className="chat-section">
        <div className="chat-header">
          Chat | Room: {roomDetails.roomId} ({gameType})
        </div>
        <div className="chat-messages">
          {messages.map(m => (
            <div key={m.id} className={`message ${m.sender === 'self' ? 'msg-self' : m.sender === 'system' ? 'msg-system' : 'msg-opponent'}`}>
              <em>{m.sender === 'system' ? '' : m.sender === 'self' ? 'You: ' : `${m.sender}: `}</em>{m.text}
            </div>
          ))}
        </div>
        <form className="chat-input-area" onSubmit={handleSend}>
          <input
            type="text"
            className="chat-input"
            value={inputText}
            onChange={(e) => setInputText(e.target.value)}
            placeholder="Type a message..."
          />
          <button type="submit" className="chat-send">Send</button>
        </form>
      </div>
    </div>
  );
};

export default GameRoom;
