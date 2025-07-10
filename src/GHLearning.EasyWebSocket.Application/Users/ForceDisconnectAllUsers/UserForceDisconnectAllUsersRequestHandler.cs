using GHLearning.EasyWebSocket.Core.Users;
using MediatR;

namespace GHLearning.EasyWebSocket.Application.Users.ForceDisconnectAllUsers;

internal class UserForceDisconnectAllUsersRequestHandler(IUserWebSocketService userWebSocket) : IRequestHandler<UserForceDisconnectAllUsersRequest>
{
	public Task Handle(UserForceDisconnectAllUsersRequest request, CancellationToken cancellationToken)
		=> userWebSocket.ForceDisconnectAllUsersAsync(cancellationToken);
}
