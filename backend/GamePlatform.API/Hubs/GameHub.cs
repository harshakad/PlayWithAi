using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace GamePlatform.API.Hubs;

public class GameHub : Hub
{
    public async Task JoinRoom(string roomId, string userName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        await Clients.Group(roomId).SendAsync("UserJoined", userName);
    }

    public async Task SendMessage(string roomId, string sender, string message)
    {
        await Clients.Group(roomId).SendAsync("ReceiveMessage", sender, message);
    }

    public async Task MakeMove(string roomId, object moveData)
    {
        // Broadcast the move to all other clients in the room
        await Clients.OthersInGroup(roomId).SendAsync("ReceiveMove", moveData);
    }

    public async Task EndTurn(string roomId, string nextTurn)
    {
        await Clients.Group(roomId).SendAsync("ReceiveTurnUpdate", nextTurn);
    }
}
