using MediatR;

namespace GHLearning.EasyWebSocket.Application.Users.ForceDisconnectUser;
public record UserForceDisconnectUserRequest(
	string UserId) : IRequest;
