using GHLearning.EasyWebSocket.Application.Users.Login;
using GHLearning.EasyWebSocket.SharedKernel.Abstractions.Authentication;
using NSubstitute;

namespace GHLearning.EasyWebSocket.ApplicationTests.Users.Login;

public class UserLoginRequestHandlerTests
{
	[Fact]
	public async Task Handle_ShouldCallForceDisconnectUserAsync_WhenRequested()
	{
		// Arrange
		var fakeTokenProvider = Substitute.For<ITokenProvider>();

		var request = new UserLoginRequest(
			Account: "account",
			Password: "password");

		var handler = new UserLoginRequestHandler(
			fakeTokenProvider);

		var token = "generated";
		_ = fakeTokenProvider
			.GenerateToken(
			account: Arg.Is(request.Account))
			.Returns(token);

		// Act
		var actual = await handler.Handle(request, CancellationToken.None).ConfigureAwait(false);

		// Assert
		Assert.NotNull(actual);
		Assert.Equal(token, actual.Token);
	}
}
