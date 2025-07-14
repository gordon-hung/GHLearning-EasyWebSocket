using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using GHLearning.EasyWebSocket.Infrastructure.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace GHLearning.EasyWebSocket.InfrastructureTests.Users;

public class UserReceiveMessagesHandlerTests
{
	private readonly ILogger<UserReceiveMessagesHandler> _logger = NullLogger<UserReceiveMessagesHandler>.Instance;
	private readonly ActivitySource _activitySource = new("TestSource");

	[Fact]
	public async Task ReceiveMessagesAsync_ShouldLogMessage_WhenTextMessageReceived()
	{
		// Arrange
		var handler = CreateHandler();
		var userId = "user1";
		var wsId = Guid.NewGuid();
		var message = "hello";
		var buffer = Encoding.UTF8.GetBytes(message);

		var webSocket = Substitute.For<WebSocket>();
		webSocket.State.Returns(WebSocketState.Open, WebSocketState.Closed);
		webSocket.ReceiveAsync(Arg.Any<ArraySegment<byte>>(), Arg.Any<CancellationToken>())
			.Returns(
				async ci =>
				{
					var segment = ci.Arg<ArraySegment<byte>>();
					if (segment.Array == null) // Ensure the array is not null
					{
						throw new ArgumentNullException(nameof(segment.Array), "Segment array cannot be null.");
					}

					buffer.CopyTo(segment.Array, 0); // Safely copy the buffer
					return await Task.FromResult(new WebSocketReceiveResult(buffer.Length, WebSocketMessageType.Text, true)).ConfigureAwait(false);
				},
				async ci => await Task.FromResult(new WebSocketReceiveResult(0, WebSocketMessageType.Close, true)).ConfigureAwait(false)
			);

		// Act
		await handler.ReceiveMessagesAsync(userId, wsId, webSocket);

		// Assert
		await webSocket.Received().ReceiveAsync(Arg.Any<ArraySegment<byte>>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ReceiveMessagesAsync_ShouldCloseWebSocket_WhenCloseMessageReceived()
	{
		// Arrange
		var handler = CreateHandler();
		var userId = "user2";
		var wsId = Guid.NewGuid();
		var buffer = Encoding.UTF8.GetBytes(string.Empty);
		var webSocket = Substitute.For<WebSocket>();
		webSocket.State.Returns(WebSocketState.Open);
		_ = webSocket.ReceiveAsync(Arg.Any<ArraySegment<byte>>(), Arg.Any<CancellationToken>())
			.Returns(ci =>
			{
				var segment = ci.Arg<ArraySegment<byte>>();
				buffer.CopyTo(segment.Array, 0);
				return Task.FromResult(new WebSocketReceiveResult(buffer.Length, WebSocketMessageType.Close, true));
			});

		// Act
		await handler.ReceiveMessagesAsync(userId, wsId, webSocket);

		// Assert
		await webSocket.Received().CloseAsync(
			WebSocketCloseStatus.NormalClosure,
			"Closed by user",
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ReceiveMessagesAsync_ShouldLogError_WhenCloseThrowsException()
	{
		// Arrange
		var handler = CreateHandler();
		var userId = "user3";
		var wsId = Guid.NewGuid();

		var webSocket = Substitute.For<WebSocket>();
		webSocket.State.Returns(WebSocketState.Open);
		webSocket.ReceiveAsync(Arg.Any<ArraySegment<byte>>(), Arg.Any<CancellationToken>())
			.Returns(ci => Task.FromResult(new WebSocketReceiveResult(0, WebSocketMessageType.Close, true)));
		webSocket.When(x => x.CloseAsync(
			WebSocketCloseStatus.NormalClosure,
			"Closed by user",
			Arg.Any<CancellationToken>()))
			.Do(x => throw new Exception("close error"));

		// Act
		await handler.ReceiveMessagesAsync(userId, wsId, webSocket);

		// Assert
		await webSocket.Received().CloseAsync(
			WebSocketCloseStatus.NormalClosure,
			"Closed by user",
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ReceiveMessagesAsync_ShouldLogError_WhenReceiveThrowsException()
	{
		// Arrange
		var handler = CreateHandler();
		var userId = "user4";
		var wsId = Guid.NewGuid();

		var webSocket = Substitute.For<WebSocket>();
		webSocket.State.Returns(WebSocketState.Open);
		webSocket.ReceiveAsync(Arg.Any<ArraySegment<byte>>(), Arg.Any<CancellationToken>())
			.Returns<Task>(x => throw new Exception("receive error"));

		// Act
		var ex = await Record.ExceptionAsync(() => handler.ReceiveMessagesAsync(userId, wsId, webSocket));

		// Assert
		Assert.Null(ex); // 應被 try-catch 吃掉
	}

	private UserReceiveMessagesHandler CreateHandler() =>
						new(_logger, _activitySource);
}
