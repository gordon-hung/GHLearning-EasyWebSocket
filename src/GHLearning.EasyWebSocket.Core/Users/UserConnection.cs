using System.Net.WebSockets;

namespace GHLearning.EasyWebSocket.Core.Users;
public class UserConnection(string userId)
{
	public string UserId { get; } = userId;
	private Dictionary<Guid, WebSocket> _connections { get; } = [];

	// 取得所有的 WebSocket GUID
	public IEnumerable<Guid> GetWebSocketIds() => _connections.Keys;

	// 添加 WebSocket 連線
	public void AddConnection(Guid webSocketId, WebSocket webSocket)
	{
		_connections.TryAdd(webSocketId, webSocket); // 使用 TryAdd 來簡化檢查
	}

	// 檢查 WebSocket 是否仍然連線
	public bool IsConnected(Guid webSocketId)
	{
		return _connections.TryGetValue(webSocketId, out var webSocket) && webSocket.State == WebSocketState.Open;
	}

	// 移除 WebSocket 連線
	public async Task RemoveConnectionAsync(Guid webSocketId)
	{
		if (_connections.TryGetValue(webSocketId, out var webSocket))
		{
			if (webSocket.State == WebSocketState.Open)
			{
				// 關閉 WebSocket 連線
				await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "強制下線", CancellationToken.None).ConfigureAwait(false);
			}
			webSocket.Dispose();
			_connections.Remove(webSocketId);
		}
	}

	// 發送訊息到特定的 WebSocket
	public async Task SendMessageAsync(Guid webSocketId, string message)
	{
		if (_connections.TryGetValue(webSocketId, out var webSocket) && webSocket.State == WebSocketState.Open)
		{
			var buffer = System.Text.Encoding.UTF8.GetBytes(message);
			await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
		}
	}
}
