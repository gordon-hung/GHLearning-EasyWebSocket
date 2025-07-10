using GHLearning.EasyWebSocket.Application.Users.ForceDisconnectUser;
using GHLearning.EasyWebSocket.Core.Users;
using NSubstitute;

namespace GHLearning.EasyWebSocket.ApplicationTests.Users.ForceDisconnectUser;
public class UserForceDisconnectUserRequestHandlerTests
{
	[Fact]
	public async Task Handle_ShouldCallForceDisconnectUserAsync_WhenRequested()
	{
		// Arrange
		var fakeUserWebSocketService = Substitute.For<IUserWebSocketService>();

		var request = new UserForceDisconnectUserRequest(
			UserId: "userId");

		var handler = new UserForceDisconnectUserRequestHandler(
			fakeUserWebSocketService);

		// Act
		await handler.Handle(request, CancellationToken.None);

		// Assert
		_ = fakeUserWebSocketService
			.Received()
			.ForceDisconnectUserAsync(
			userId: Arg.Is(request.UserId),
			cancellationToken: Arg.Any<CancellationToken>());
	}
}
