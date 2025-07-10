using GHLearning.EasyWebSocket.Core.Users;
using MediatR;

namespace GHLearning.EasyWebSocket.Application.Users.SendMessageToUser;

internal class UserSendMessageToUserRequestHandler(
	IUserWebSocketService userWebSocket) : IRequestHandler<UserSendMessageToUserRequest>
{
	public Task Handle(UserSendMessageToUserRequest notification, CancellationToken cancellationToken)
		=> userWebSocket.SendMessageToUserAsync(notification.UserId, notification.Risk, notification.Message, cancellationToken);
}
