using System.Net.WebSockets;

namespace GHLearning.EasyWebSocket.Core.Users;

public interface IUserReceiveMessagesHandler
{
	public Task ReceiveMessagesAsync(string userId, Guid webSocketId, WebSocket webSocket, CancellationToken cancellationToken = default);
}
