using Microsoft.AspNetCore.Http;
using GHLearning.EasyWebSocket.Core.Users.Models;

namespace GHLearning.EasyWebSocket.Core.Users;

public interface IUserWebSocketService
{
	/// <summary>
	/// Handles the web socket connection asynchronous.
	/// </summary>
	/// <param name="context">The context.</param>
	/// <returns></returns>
	Task HandleWebSocketConnectionAsync(HttpContext context);

	/// <summary>
	/// Sends the message to all asynchronous.
	/// </summary>
	/// <param name="risk">The risk.</param>
	/// <param name="message">The message.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns></returns>
	Task SendMessageToAllAsync(UserMessageRisk risk, string message, CancellationToken cancellationToken = default);

	/// <summary>
	/// Forces the disconnect user asynchronous.
	/// </summary>
	/// <param name="userId">The user identifier.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns></returns>
	Task ForceDisconnectUserAsync(string userId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Forces the disconnect all users asynchronous.
	/// </summary>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns></returns>
	Task ForceDisconnectAllUsersAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Sends the message to user asynchronous.
	/// </summary>
	/// <param name="userId">The user identifier.</param>
	/// <param name="risk">The risk.</param>
	/// <param name="message">The message.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns></returns>
	Task SendMessageToUserAsync(string userId, UserMessageRisk risk, string message, CancellationToken cancellationToken = default);

	/// <summary>
	/// Monitors the connections asynchronous.
	/// </summary>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns></returns>
	Task MonitorConnectionsAsync(CancellationToken cancellationToken = default);
}

