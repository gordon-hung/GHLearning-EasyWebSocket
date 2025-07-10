using GHLearning.EasyWebSocket.SharedKernel.Abstractions.Authentication;
using MediatR;

namespace GHLearning.EasyWebSocket.Application.Users.Login;

internal class UserLoginRequestHandler(
	ITokenProvider tokenProvider) : IRequestHandler<UserLoginRequest, UserLoginResponse>
{
	public Task<UserLoginResponse> Handle(UserLoginRequest request, CancellationToken cancellationToken)
	{
		/*
		 * 帳號驗證
		 */

		return Task.FromResult(
			new UserLoginResponse(
				Token: tokenProvider.GenerateToken(request.Account)));
	}
}
