import React, { useState, useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import Board, { generateInitialBoard } from './Board';
import './GameRoom.css';

const GameRoom = ({ gameType, onBack }) => {
  const [messages, setMessages] = useState([
    { id: 1, sender: 'system', text: 'Welcome to the room! Waiting for opponent...' }
  ]);
  const [inputText, setInputText] = useState('');
  const [boardState, setBoardState] = useState(generateInitialBoard(gameType));
  const [connection, setConnection] = useState(null);
  const roomId = 'test-room-1'; // Hardcoded for demo, normally dynamic

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
          connection.invoke('JoinRoom', roomId);

          connection.on('UserJoined', (msg) => {
            setMessages(prev => [...prev, { id: Date.now(), sender: 'system', text: `A user has joined the room.` }]);
          });

          connection.on('ReceiveMessage', (sender, message) => {
            setMessages(prev => [...prev, { id: Date.now(), sender: sender === connection.connectionId ? 'self' : 'opponent', text: message }]);
          });

          connection.on('ReceiveMove', (moveData) => {
            // Apply opponent's move locally
            applyMoveLocally(moveData.sourceRow, moveData.sourceCol, moveData.targetRow, moveData.targetCol);
          });
        })
        .catch(e => console.log('Connection failed: ', e));
    }
  }, [connection]);

  const applyMoveLocally = (sourceRow, sourceCol, targetRow, targetCol) => {
    setBoardState(prevBoard => {
      const newBoard = prevBoard.map(row => [...row]);
      newBoard[targetRow][targetCol] = newBoard[sourceRow][sourceCol];
      newBoard[sourceRow][sourceCol] = null;
      return newBoard;
    });
  };

  const handleMove = (sourceRow, sourceCol, targetRow, targetCol) => {
    // 1. Move locally
    applyMoveLocally(sourceRow, sourceCol, targetRow, targetCol);
    // 2. Broadcast via SignalR
    if (connection && connection.state === signalR.HubConnectionState.Connected) {
      connection.invoke('MakeMove', roomId, { sourceRow, sourceCol, targetRow, targetCol })
        .catch(err => console.error("Move failed to send", err));
    }
  };

  const handleSend = (e) => {
    e.preventDefault();
    if (!inputText.trim()) return;
    
    if (connection && connection.state === signalR.HubConnectionState.Connected) {
      connection.invoke('SendMessage', roomId, connection.connectionId, inputText);
    } else {
      setMessages([...messages, { id: Date.now(), sender: 'self', text: inputText }]); // Fallback if offline
    }
    
    setInputText('');
  };

  return (
    <div className="room-container">
      <div className="board-section">
        <button className="back-button" onClick={onBack}>← Back</button>
        <Board boardState={boardState} onMove={handleMove} />
      </div>
      <div className="chat-section">
        <div className="chat-header">
          {gameType === 'chess' ? '♟️ Chess' : '🔴 Checkers'} Chat
        </div>
        <div className="chat-messages">
          {messages.map(m => (
            <div key={m.id} className={`message ${m.sender === 'self' ? 'msg-self' : m.sender === 'system' ? 'msg-system' : 'msg-opponent'}`}>
              <em>{m.sender === 'system' ? '' : m.sender === 'self' ? 'You: ' : 'Opponent: '}</em>{m.text}
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
