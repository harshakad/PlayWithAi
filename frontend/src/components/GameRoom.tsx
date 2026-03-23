import React, { useState, useEffect } from 'react';
import * as signalR from '@microsoft/signalr';
import { GameFactory } from './GameFactory';
import { MoveData } from './Board';

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
  const roomId = roomDetails.roomId;

  // Setup SignalR connection
  useEffect(() => {
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl("http://localhost:5195/gamehub")
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
          console.log('Connected to server!');
          connection.invoke('JoinRoom', roomId, roomDetails.userName);

          connection.on('UserJoined', (joinedUserName: string) => {
            if (joinedUserName !== roomDetails.userName) {
              setMessages(prev => [...prev, { id: Date.now(), sender: 'system', text: `${joinedUserName} has joined the room.` }]);
            }
          });

          connection.on('ReceiveMessage', (sender: string, message: string) => {
            setMessages(prev => [...prev, { id: Date.now(), sender: sender === roomDetails.userName ? 'self' : sender, text: message }]);
          });

          connection.on('ReceiveMove', (moveData: { sourceRow: number, sourceCol: number, targetRow: number, targetCol: number }) => {
            // Apply opponent's move via prop
            setPendingMove({ ...moveData, id: Date.now() });
          });

          connection.on('ReceiveTurnUpdate', (nextTurn: string) => {
            setCurrentTurn(nextTurn);
            setMessages(prev => [...prev, { id: Date.now(), sender: 'system', text: `It is now ${nextTurn}'s turn.` }]);
          });
        })
        .catch(e => console.log('Connection failed: ', e));
    }
  }, [connection]);

  const handleMoveFinished = (sourceRow: number, sourceCol: number, targetRow: number, targetCol: number) => {
    if (roomDetails.role !== 'player') return;

    // Broadcast via SignalR
    if (connection && connection.state === signalR.HubConnectionState.Connected) {
      connection.invoke('MakeMove', roomId, { sourceRow, sourceCol, targetRow, targetCol })
        .catch(err => console.error("Move failed to send", err));
    }
  };

  const endTurn = () => {
    const nextTurn = currentTurn === 'first' ? 'second' : 'first';
    setCurrentTurn(nextTurn);

    if (connection && connection.state === signalR.HubConnectionState.Connected) {
      connection.invoke('EndTurn', roomId, nextTurn)
        .catch(err => console.error("EndTurn failed to send", err));
    }
  };

  const handleSend = (e: React.SyntheticEvent) => {
    e.preventDefault();
    if (!inputText.trim()) return;

    if (connection && connection.state === signalR.HubConnectionState.Connected) {
      connection.invoke('SendMessage', roomId, roomDetails.userName, inputText);
    } else {
      setMessages([...messages, { id: Date.now(), sender: 'self', text: inputText }]); // Fallback if offline
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
              canEndTurn={roomDetails.role === 'player' && currentTurn === roomDetails.side}
            />
          ) : null;
        })()}

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
