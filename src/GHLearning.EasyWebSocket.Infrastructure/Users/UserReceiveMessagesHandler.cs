using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using GHLearning.EasyWebSocket.Core.Users;
using Microsoft.Extensions.Logging;

namespace GHLearning.EasyWebSocket.Infrastructure.Users;

internal sealed class UserReceiveMessagesHandler(
	ILogger<UserReceiveMessagesHandler> logger,
	ActivitySource activitySource) : IUserReceiveMessagesHandler
{
	public async Task ReceiveMessagesAsync(string userId, Guid webSocketId, WebSocket webSocket, CancellationToken cancellationToken = default)
	{
		using var activity = activitySource.StartActivity("WebSocket Receive");
		activity?.SetTag("websocket.namespace", typeof(UserWebSocketService).Namespace);
		activity?.SetTag("websocket.id", webSocketId.ToString());

		var buffer = new byte[1024 * 8];
		try
		{
			while (webSocket.State == WebSocketState.Open)
			{
				var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);
				if (result.MessageType == WebSocketMessageType.Close)
				{
					try
					{
						await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by user", CancellationToken.None).ConfigureAwait(false);

						logger.LogInformation("用戶 {UserId} 的 WebSocket ID: {WebSocketId} 已成功關閉", userId, webSocketId);
					}
					catch (Exception ex)
					{
						logger.LogError(ex, "用戶 {UserId} WebSocket ID: {WebSocketId} 關閉錯誤", userId, webSocketId);
					}

					break;
				}
				else
				{
					string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
					logger.LogInformation("收到用戶 {UserId} WebSocket ID: {WebSocketId} 訊息: {Message}", userId, webSocketId, message);
				}
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "WebSocket 連線錯誤，用戶 {UserId} WebSocket ID: {WebSocketId}", userId, webSocketId);
		}
	}
}
