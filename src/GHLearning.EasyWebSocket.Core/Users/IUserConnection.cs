using System.Net.WebSockets;

namespace GHLearning.EasyWebSocket.Core.Users;

public interface IUserConnection
{
	string UserId { get; }

	IEnumerable<Guid> GetWebSocketIds();

	void AddConnection(Guid webSocketId, WebSocket webSocket);

	bool IsConnected(Guid webSocketId);

	Task RemoveConnectionAsync(Guid webSocketId);

	Task SendMessageAsync(Guid webSocketId, string message);
}
