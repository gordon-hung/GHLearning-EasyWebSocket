using GHLearning.EasyWebSocket.Application.Users.SendMessageToUser;
using GHLearning.EasyWebSocket.Core.Users;
using NSubstitute;

namespace GHLearning.EasyWebSocket.ApplicationTests.Users.SendMessageToUser;
public class UserSendMessageToUserRequestHandlerTests
{
	[Fact]
	public async Task Handle_ShouldCallSendMessageToUserAsync_WhenRequested()
	{
		// Arrange
		var fakeUserWebSocketService = Substitute.For<IUserWebSocketService>();

		var request = new UserSendMessageToUserRequest(
			UserId: "userId",
			Risk: Core.Users.Models.UserMessageRisk.Low,
			Message: "message");

		var handler = new UserSendMessageToUserRequestHandler(
			fakeUserWebSocketService);

		// Act
		await handler.Handle(request, CancellationToken.None);

		// Assert
		_ = fakeUserWebSocketService
			.Received()
			.SendMessageToUserAsync(
			userId: Arg.Is(request.UserId),
			risk: Arg.Is(request.Risk),
			message: Arg.Is(request.Message),
			cancellationToken: Arg.Any<CancellationToken>());
	}
}
