import React, { useState, useEffect } from 'react';
import * as signalR from '@microsoft/signalr';
import Board, { generateInitialBoard, GamePiece } from './Board';
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
  const [boardState, setBoardState] = useState<(GamePiece | null)[][]>(generateInitialBoard(gameType));
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
  const [currentTurn, setCurrentTurn] = useState<string>(gameType === 'chess' ? 'white' : 'black');
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
            // Apply opponent's move locally
            applyMoveLocally(moveData.sourceRow, moveData.sourceCol, moveData.targetRow, moveData.targetCol);
          });

          connection.on('ReceiveTurnUpdate', (nextTurn: string) => {
            setCurrentTurn(nextTurn);
            setMessages(prev => [...prev, { id: Date.now(), sender: 'system', text: `It is now ${nextTurn}'s turn.` }]);
          });
        })
        .catch(e => console.log('Connection failed: ', e));
    }
  }, [connection]);

  const applyMoveLocally = (sourceRow: number, sourceCol: number, targetRow: number, targetCol: number) => {
    setBoardState(prevBoard => {
      const newBoard = prevBoard.map(row => [...row]);
      newBoard[targetRow][targetCol] = newBoard[sourceRow][sourceCol];
      newBoard[sourceRow][sourceCol] = null;
      return newBoard;
    });
  };

  const handleMove = (sourceRow: number, sourceCol: number, targetRow: number, targetCol: number) => {
    if (roomDetails.role !== 'player') return;
    if (currentTurn !== roomDetails.side) return;
    const piece = boardState[sourceRow][sourceCol];
    if (!piece || piece.color !== roomDetails.side) return;
    
    // 1. Move locally
    applyMoveLocally(sourceRow, sourceCol, targetRow, targetCol);
    
    // 2. Broadcast via SignalR
    if (connection && connection.state === signalR.HubConnectionState.Connected) {
      connection.invoke('MakeMove', roomId, { sourceRow, sourceCol, targetRow, targetCol })
        .catch(err => console.error("Move failed to send", err));
      
      // Auto-end turn for chess
      if (gameType === 'chess') {
        const nextTurn = roomDetails.side === 'white' ? 'black' : 'white';
        setCurrentTurn(nextTurn);
        connection.invoke('EndTurn', roomId, nextTurn)
          .catch(err => console.error("EndTurn failed to send", err));
      }
    }
  };

  const endTurn = () => {
    if (gameType !== 'checkers' || currentTurn !== roomDetails.side) return;
    
    const nextTurn = roomDetails.side === 'black' ? 'red' : 'black';
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
        <Board
          boardState={boardState}
          onMove={handleMove}
          allowedSide={roomDetails.role === 'player' && currentTurn === roomDetails.side ? roomDetails.side : 'none'}
        />
        {gameType === 'checkers' && roomDetails.role === 'player' && (
          <button 
            className="end-turn-button" 
            onClick={endTurn}
            disabled={currentTurn !== roomDetails.side}
          >
            End Turn
          </button>
        )}
      </div>
      <div className="chat-section">
        <div className="chat-header">
          {gameType === 'chess' ? '♟️ Chess' : '🔴 Checkers'} Chat | Room: {roomDetails.roomId}
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
