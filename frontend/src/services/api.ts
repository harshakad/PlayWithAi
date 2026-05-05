const BASE_URL = 'http://localhost:5039/api';

export interface MoveResult {
  isSuccess: boolean;
  errorMessage?: string;
  isGameOver: boolean;
  gameOverReason?: string;
}

export const api = {
  createRoom: async (type: string, name: string, playAgainstAi: boolean = false) => {
    const response = await fetch(`${BASE_URL}/Games/Create?type=${type}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name, isAgainstAi: playAgainstAi })
    });
    if (!response.ok) throw new Error('Failed to create room');
    return response.json();
  },

  joinRoom: async (roomId: string, playerName: string, side: string) => {
    const response = await fetch(`${BASE_URL}/Games/${roomId}/Join?playerName=${playerName}&side=${side}`, {
      method: 'POST'
    });
    if (!response.ok) {
        const error = await response.text();
        throw new Error(error || 'Failed to join room');
    }
    return response.json();
  },

  getRoom: async (roomId: string) => {
    const response = await fetch(`${BASE_URL}/Games/Get/${roomId}`);
    if (!response.ok) throw new Error('Room not found');
    return response.json();
  },

  makeMove: async (roomId: string, playerName: string, move: { sourceRow: number, sourceCol: number, targetRow: number, targetCol: number }): Promise<MoveResult> => {
    const response = await fetch(`${BASE_URL}/Games/${roomId}/Board/Move`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ playerName, ...move })
    });
    if (!response.ok) {
        const error = await response.text();
        throw new Error(error || 'Failed to make move');
    }
    return response.json();
  },

  endTurn: async (roomId: string): Promise<MoveResult> => {
    const response = await fetch(`${BASE_URL}/Games/${roomId}/EndTurn`, {
      method: 'POST'
    });
    if (!response.ok) {
        const error = await response.text();
        throw new Error(error || 'Failed to end turn');
    }
    return response.json();
  }
};

