using GHLearning.EasyWebSocket.Application.Users.ForceDisconnectAllUsers;
using GHLearning.EasyWebSocket.Core.Users;
using NSubstitute;

namespace GHLearning.EasyWebSocket.ApplicationTests.Users.ForceDisconnectAllUsers;

public class UserForceDisconnectAllUsersRequestHandlerTests
{
	[Fact]
	public async Task Handle_ShouldCallForceDisconnectAllUsersAsync_WhenRequested()
	{
		// Arrange
		var fakeUserWebSocketService = Substitute.For<IUserWebSocketService>();

		var handler = new UserForceDisconnectAllUsersRequestHandler(
			fakeUserWebSocketService);

		// Act
		await handler.Handle(new(), CancellationToken.None);

		// Assert
		_ = fakeUserWebSocketService
			.Received()
			.ForceDisconnectAllUsersAsync(
			cancellationToken: Arg.Any<CancellationToken>());
	}
}
