using GHLearning.EasyWebSocket.Core.Users;
using GHLearning.EasyWebSocket.Core.Users.Models;
using Microsoft.AspNetCore.Http;

namespace GHLearning.EasyWebSocket.Infrastructure.Users;

internal sealed class UserWebSocketService(
	IUserConnectionManager userConnectionManager) : IUserWebSocketService
{
	public Task HandleWebSocketConnectionAsync(HttpContext context)
		=> userConnectionManager.HandleWebSocketConnectionAsync(context);

	public Task SendMessageToAllAsync(UserMessageRisk risk, string message, CancellationToken cancellationToken = default)
		=> userConnectionManager.SendMessageToAllAsync(risk, message, cancellationToken);

	public Task ForceDisconnectUserAsync(string userId, CancellationToken cancellationToken = default)
		=> userConnectionManager.ForceDisconnectUserAsync(userId, cancellationToken);

	public Task ForceDisconnectAllUsersAsync(CancellationToken cancellationToken = default)
		=> userConnectionManager.ForceDisconnectAllUsersAsync(cancellationToken);

	public Task SendMessageToUserAsync(string userId, UserMessageRisk risk, string message, CancellationToken cancellationToken = default)
		=> userConnectionManager.SendMessageToUserAsync(userId, risk, message, cancellationToken);

	public Task MonitorConnectionsAsync(CancellationToken cancellationToken = default)
		=> userConnectionManager.MonitorConnectionsAsync(cancellationToken);
}
