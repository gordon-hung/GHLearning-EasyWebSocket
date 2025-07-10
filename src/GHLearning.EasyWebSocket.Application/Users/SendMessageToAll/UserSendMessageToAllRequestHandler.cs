using GHLearning.EasyWebSocket.Core.Users;
using MediatR;

namespace GHLearning.EasyWebSocket.Application.Users.SendMessageToAll;

internal class UserSendMessageToAllRequestHandler(
	IUserWebSocketService userWebSocket) : IRequestHandler<UserSendMessageToAllRequest>
{
	public Task Handle(UserSendMessageToAllRequest notification, CancellationToken cancellationToken)
		=> userWebSocket.SendMessageToAllAsync(notification.Risk, notification.Message, cancellationToken);
}
