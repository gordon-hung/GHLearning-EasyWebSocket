using MediatR;

namespace GHLearning.EasyWebSocket.Application.Users.Login;
public record UserLoginRequest(
	string Account,
	string Password) : IRequest<UserLoginResponse>;
