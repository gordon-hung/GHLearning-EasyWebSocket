using GHLearning.EasyWebSocket.Application.Users.SendMessageToAll;
using GHLearning.EasyWebSocket.Core.Users;
using NSubstitute;

namespace GHLearning.EasyWebSocket.ApplicationTests.Users.SendMessageToAll;
public class UserSendMessageToAllRequestHandlerTests
{
	[Fact]
	public async Task Handle_ShouldCallSendMessageToAllAsync_WhenRequested()
	{
		// Arrange
		var fakeUserWebSocketService = Substitute.For<IUserWebSocketService>();

		var request = new UserSendMessageToAllRequest(
			Risk: Core.Users.Models.UserMessageRisk.Low,
			Message: "message");

		var handler = new UserSendMessageToAllRequestHandler(
			fakeUserWebSocketService);

		// Act
		await handler.Handle(request, CancellationToken.None);

		// Assert
		_ = fakeUserWebSocketService
			.Received()
			.SendMessageToAllAsync(
			risk: Arg.Is(request.Risk),
			message: Arg.Is(request.Message),
			cancellationToken: Arg.Any<CancellationToken>());
	}
}
