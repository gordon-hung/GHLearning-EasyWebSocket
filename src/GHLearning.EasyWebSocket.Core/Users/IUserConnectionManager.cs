using GHLearning.EasyWebSocket.Core.Users.Models;
using Microsoft.AspNetCore.Http;

namespace GHLearning.EasyWebSocket.Core.Users;

public interface IUserConnectionManager
{
	Task HandleWebSocketConnectionAsync(HttpContext context);

	Task SendMessageToAllAsync(UserMessageRisk risk, string message, CancellationToken cancellationToken = default);

	Task ForceDisconnectUserAsync(string userId, CancellationToken cancellationToken = default);

	Task ForceDisconnectAllUsersAsync(CancellationToken cancellationToken = default);

	Task SendMessageToUserAsync(string userId, UserMessageRisk risk, string message, CancellationToken cancellationToken = default);

	Task MonitorConnectionsAsync(CancellationToken cancellationToken = default);
}
