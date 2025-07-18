﻿using GHLearning.EasyWebSocket.Core.Users;
using MediatR;

namespace GHLearning.EasyWebSocket.Application.Users.ForceDisconnectUser;

internal class UserForceDisconnectUserRequestHandler(
	IUserWebSocketService userWebSocket) : IRequestHandler<UserForceDisconnectUserRequest>
{
	public Task Handle(UserForceDisconnectUserRequest request, CancellationToken cancellationToken)
		=> userWebSocket.ForceDisconnectUserAsync(request.UserId, cancellationToken);
}
