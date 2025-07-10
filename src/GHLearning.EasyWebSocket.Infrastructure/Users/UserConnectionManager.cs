using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using GHLearning.EasyWebSocket.Core.Users;
using GHLearning.EasyWebSocket.Core.Users.Models;
using GHLearning.EasyWebSocket.SharedKernel.Abstractions.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GHLearning.EasyWebSocket.Infrastructure.Users;

public class UserConnectionManager(
	ILogger<UserConnectionManager> logger,
	ITokenProvider tokenProvider,
	TimeProvider timeProvider,
	ActivitySource activitySource) : IUserConnectionManager
{
	private readonly ConcurrentDictionary<string, IUserConnection> _userConnections = new();

	public async Task HandleWebSocketConnectionAsync(HttpContext context)
	{
		if (!context.WebSockets.IsWebSocketRequest)
		{
			context.Response.StatusCode = (int)HttpStatusCode.UpgradeRequired;
			logger.LogWarning("非 WebSocket 請求，請求來自: {RemoteAddress}", context.Connection.RemoteIpAddress);
			return;
		}

		var token = context.Request.Query["token"].ToString();
		if (string.IsNullOrEmpty(token))
		{
			context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
			logger.LogWarning("WebSocket 連線失敗，缺少 token，請求來自: {RemoteAddress}", context.Connection.RemoteIpAddress);
			return;
		}

		var userId = tokenProvider.ValidateToken(token);
		if (string.IsNullOrEmpty(userId))
		{
			context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
			logger.LogWarning("WebSocket 連線失敗，Token 驗證失敗，請求來自: {RemoteAddress}", context.Connection.RemoteIpAddress);
			return;
		}

		var webSocket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
		var webSocketId = Guid.NewGuid();
		var userConnection = _userConnections.GetOrAdd(userId, _ => new UserConnection(userId));

		userConnection.AddConnection(webSocketId, webSocket);

		logger.LogInformation("用戶 {userId} 連線，WebSocket ID: {webSocketId}", userId, webSocketId);

		await ReceiveMessagesAsync(webSocket, userId, webSocketId).ConfigureAwait(false);
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
			if (_userConnections.TryGetValue(userId, out IUserConnection? userConnection))
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
				await ForceDisconnectUserAsync(userId, cancellationToken).ConfigureAwait(false);
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
		var checkInterval = TimeSpan.FromSeconds(30); // 每 30 秒檢查一次連線狀態

		while (!cancellationToken.IsCancellationRequested)
		{
			var userMessage = new UserMessage(
				Risk: UserMessageRisk.Low,
				Type: UserMessageType.Private,
				SendAt: timeProvider.GetUtcNow(),
				Message: "Ping");

			var activeConnections = 0;

			logger.LogInformation("定期監控 WebSocket 連線");

			foreach (var userId in _userConnections.Keys)
			{
				if (_userConnections.TryGetValue(userId, out IUserConnection? userConnection))
				{
					foreach (var webSocketId in userConnection.GetWebSocketIds())
					{
						try
						{
							// 檢查連線狀態
							if (userConnection.IsConnected(webSocketId))
							{
								// 如果連線正常，發送 "Ping" 訊息
								await userConnection.SendMessageAsync(webSocketId, JsonSerializer.Serialize(userMessage)).ConfigureAwait(false);
								activeConnections++;
							}
							else
							{
								// 如果 WebSocket 連線斷開，則移除該連線
								await userConnection.RemoveConnectionAsync(webSocketId).ConfigureAwait(false);
							}
						}
						catch (Exception ex)
						{
							logger.LogError(ex, "檢查 WebSocket 連線時發生錯誤，用戶 {UserId} WebSocket ID: {WebSocketId}", userId, webSocketId);
						}
					}
				}
			}

			logger.LogInformation("當前活躍 WebSocket 連線數: {Quantity}", activeConnections);

			await Task.Delay(checkInterval, cancellationToken).ConfigureAwait(false);
		}
	}

	private async Task ReceiveMessagesAsync(WebSocket webSocket, string userId, Guid webSocketId)
	{
		using var activity = activitySource.StartActivity("WebSocket Receive");
		activity?.SetTag("websocket.namespace", typeof(UserWebSocketService).Namespace);
		activity?.SetTag("websocket.id", webSocketId.ToString());

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
}
