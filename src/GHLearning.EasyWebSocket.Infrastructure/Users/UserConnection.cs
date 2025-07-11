using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using GHLearning.EasyWebSocket.Core.Users;

namespace GHLearning.EasyWebSocket.Infrastructure.Users;

public class UserConnection(string userId) : IUserConnection
{
	private readonly ConcurrentDictionary<Guid, WebSocket> _connections = new();
	public string UserId { get; } = userId;

	public IEnumerable<Guid> GetWebSocketIds() => _connections.Keys;

	public void AddConnection(Guid webSocketId, WebSocket webSocket)
	{
		_connections.TryAdd(webSocketId, webSocket);
	}

	public bool IsConnected(Guid webSocketId)
	{
		return _connections.TryGetValue(webSocketId, out var webSocket) && webSocket.State == WebSocketState.Open;
	}

	public async Task RemoveConnectionAsync(Guid webSocketId)
	{
		if (_connections.TryGetValue(webSocketId, out var webSocket))
		{
			try
			{
				if (webSocket.State == WebSocketState.Open)
				{
					await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "強制下線", CancellationToken.None).ConfigureAwait(false);
				}
			}
			catch (Exception ex)
			{
				// 異常處理
				Console.Error.WriteLine($"移除連線時發生錯誤: {ex.Message}");
			}
			finally
			{
				_connections.TryRemove(webSocketId, out _);
				webSocket.Dispose();
			}
		}
	}

	public async Task SendMessageAsync(Guid webSocketId, string message)
	{
		if (string.IsNullOrEmpty(message))
			return;

		if (_connections.TryGetValue(webSocketId, out var webSocket) && webSocket.State == WebSocketState.Open)
		{
			try
			{
				var buffer = Encoding.UTF8.GetBytes(message);
				await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"發送訊息時發生錯誤: {ex.Message}");
			}
		}
	}
}
