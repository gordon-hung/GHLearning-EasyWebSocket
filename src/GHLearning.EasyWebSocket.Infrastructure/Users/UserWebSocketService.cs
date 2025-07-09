using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using GHLearning.EasyWebSocket.Core.Users;
using GHLearning.EasyWebSocket.Core.Users.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GHLearning.EasyWebSocket.Infrastructure.Users;

internal sealed class UserWebSocketService(
	ILogger<UserWebSocketService> logger,
	TimeProvider timeProvider) : IUserWebSocketService
{
	private readonly ConcurrentDictionary<string, UserConnection> _userConnections = new();

	public async Task HandleWebSocketConnectionAsync(HttpContext context)
	{
		if (!context.WebSockets.IsWebSocketRequest)
		{
			context.Response.StatusCode = 400;
			logger.LogWarning("非 WebSocket 請求，請求來自: {RemoteAddress}", context.Connection.RemoteIpAddress);
			return;
		}

		var userId = context.Request.Query["userId"].ToString();
		if (string.IsNullOrEmpty(userId))
		{
			await context.Response.WriteAsync("缺少 userId").ConfigureAwait(false);
			await context.WebSockets.AcceptWebSocketAsync().Result.CloseAsync(WebSocketCloseStatus.PolicyViolation, "缺少 userId", CancellationToken.None).ConfigureAwait(false);
			logger.LogWarning("WebSocket 連線失敗，缺少 userId，請求來自: {RemoteAddress}", context.Connection.RemoteIpAddress);
			return;
		}

		var webSocket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
		var webSocketId = Guid.CreateVersion7(timeProvider.GetUtcNow());
		var userConnection = _userConnections.GetOrAdd(userId, _ => new UserConnection(userId));

		userConnection.AddConnection(webSocketId, webSocket);

		logger.LogInformation("用戶 {userId} 連線，WebSocket ID: {webSocketId}", userId, webSocketId);

		await ReceiveMessagesAsync(webSocket, userId, webSocketId).ConfigureAwait(false);
	}

	private async Task ReceiveMessagesAsync(WebSocket webSocket, string userId, Guid webSocketId)
	{
		var buffer = new byte[1024 * 8];
		var cancellationToken = CancellationToken.None;

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
						if (_userConnections.TryGetValue(userId, out var userConnection))
						{
							await userConnection.RemoveConnectionAsync(webSocketId).ConfigureAwait(false);
						}
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

	public async Task SendMessageToAllAsync(UserMessageRisk risk, string message, CancellationToken cancellationToken = default)
	{
		var userMessage = new UserMessage(
			Risk: risk,
			Type: UserMessageType.All,
			SendAt: timeProvider.GetUtcNow(),
			Message: message);
		foreach (var userId in _userConnections.Keys)
		{
			if (_userConnections.TryGetValue(userId, out UserConnection? userConnection))
			{
				foreach (var webSocketId in userConnection.GetWebSocketIds())
				{
					try
					{
						await userConnection.SendMessageAsync(webSocketId, JsonSerializer.Serialize(userMessage)).ConfigureAwait(false);
					}
					catch (Exception ex)
					{
						logger.LogError(ex, "發送訊息失敗 用戶 {UserId} WebSocket ID: {WebSocketId}", userId, webSocketId);
					}
				}
			}
		}
	}

	public async Task ForceDisconnectUserAsync(string userId, CancellationToken cancellationToken = default)
	{
		if (_userConnections.TryGetValue(userId, out var userConnection))
		{
			foreach (var webSocketId in userConnection.GetWebSocketIds())
			{
				try
				{
					await userConnection.RemoveConnectionAsync(webSocketId).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "強制下線用戶 {UserId} WebSocket ID: {WebSocketId} 時發生錯誤", userId, webSocketId);
				}
			}
		}
		else
		{
			logger.LogWarning("用戶 {UserId} 找不到，無法強制下線", userId);
		}
	}

	public async Task ForceDisconnectAllUsersAsync(CancellationToken cancellationToken = default)
	{
		foreach (var userId in _userConnections.Keys.ToList())
		{
			try
			{
				// 強制下線該用戶
				await ForceDisconnectUserAsync(userId).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "強制下線用戶 {UserId} 時發生錯誤", userId);
			}
		}
	}

	public async Task SendMessageToUserAsync(string userId, UserMessageRisk risk, string message, CancellationToken cancellationToken = default)
	{
		if (_userConnections.TryGetValue(userId, out var userConnection))
		{
			var userMessage = new UserMessage(
				Risk: risk,
				Type: UserMessageType.Private,
				SendAt: timeProvider.GetUtcNow(),
				Message: message);
			foreach (var webSocketId in userConnection.GetWebSocketIds())
			{
				try
				{
					await userConnection.SendMessageAsync(webSocketId, JsonSerializer.Serialize(userMessage)).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "發送訊息失敗 用戶 {UserId} WebSocket ID: {WebSocketId}", userId, webSocketId);
				}
			}
		}
		else
		{
			logger.LogWarning("用戶 {UserId} 不存在，無法發送訊息", userId);
		}
	}

	public async Task MonitorConnectionsAsync(CancellationToken cancellationToken = default)
	{
		var checkInterval = TimeSpan.FromSeconds(30);

		while (!cancellationToken.IsCancellationRequested)
		{
			var userMessage = new UserMessage(
				Risk: UserMessageRisk.Low,
				Type: UserMessageType.Private,
				SendAt: timeProvider.GetUtcNow(),
				Message: "Ping");

			var quantity = 0;

			logger.LogInformation("開始監控 WebSocket 連線");

			foreach (var userId in _userConnections.Keys)
			{
				if (_userConnections.TryGetValue(userId, out UserConnection? userConnection))
				{
					foreach (var webSocketId in userConnection.GetWebSocketIds())
					{
						try
						{
							if (userConnection.IsConnected(webSocketId))
							{
								await userConnection.SendMessageAsync(webSocketId, JsonSerializer.Serialize(userMessage)).ConfigureAwait(false);
								quantity++;
							}
							else
							{
								await userConnection.RemoveConnectionAsync(webSocketId).ConfigureAwait(false);
							}
						}
						catch (Exception ex)
						{
							logger.LogError(ex, "檢查連線時發生錯誤，用戶 {UserId} WebSocket ID: {WebSocketId}", userId, webSocketId);
						}
					}
				}
			}

			logger.LogInformation("當前活躍 WebSocket 連線數: {Quantity}", quantity);

			await Task.Delay(checkInterval, cancellationToken).ConfigureAwait(false);
		}
	}
}
