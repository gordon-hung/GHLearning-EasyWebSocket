using System.Net.WebSockets;
using System.Text;
using GHLearning.EasyWebSocket.Infrastructure.Users;
using NSubstitute;

namespace GHLearning.EasyWebSocket.InfrastructureTests.Users;
public class UserConnectionTests
{
	[Fact]
	public void AddConnection_ShouldAddWebSocket()
	{
		// Arrange
		var userId = "user1";
		var connection = new UserConnection(userId);
		var wsId = Guid.NewGuid();
		var ws = Substitute.For<WebSocket>();

		// Act
		connection.AddConnection(wsId, ws);

		// Assert
		Assert.Contains(wsId, connection.GetWebSocketIds());
	}

	[Fact]
	public void IsConnected_ShouldReturnTrue_WhenWebSocketIsOpen()
	{
		// Arrange
		var userId = "user2";
		var connection = new UserConnection(userId);
		var wsId = Guid.NewGuid();
		var ws = Substitute.For<WebSocket>();
		ws.State.Returns(WebSocketState.Open);
		connection.AddConnection(wsId, ws);

		// Act
		var result = connection.IsConnected(wsId);

		// Assert
		Assert.True(result);
	}

	[Fact]
	public void IsConnected_ShouldReturnFalse_WhenWebSocketIsNotOpen()
	{
		// Arrange
		var userId = "user3";
		var connection = new UserConnection(userId);
		var wsId = Guid.NewGuid();
		var ws = Substitute.For<WebSocket>();
		ws.State.Returns(WebSocketState.Closed);
		connection.AddConnection(wsId, ws);

		// Act
		var result = connection.IsConnected(wsId);

		// Assert
		Assert.False(result);
	}

	[Fact]
	public async Task RemoveConnectionAsync_ShouldCloseAndRemoveWebSocket()
	{
		// Arrange
		var userId = "user4";
		var connection = new UserConnection(userId);
		var wsId = Guid.NewGuid();
		var ws = Substitute.For<WebSocket>();
		ws.State.Returns(WebSocketState.Open);
		connection.AddConnection(wsId, ws);

		// Act
		await connection.RemoveConnectionAsync(wsId);

		// Assert
		await ws.Received(1).CloseAsync(WebSocketCloseStatus.NormalClosure, "強制下線", CancellationToken.None);
		ws.Received(1).Dispose();
		Assert.DoesNotContain(wsId, connection.GetWebSocketIds());
	}

	[Fact]
	public async Task RemoveConnectionAsync_ShouldDispose_WhenWebSocketIsNotOpen()
	{
		// Arrange
		var userId = "user5";
		var connection = new UserConnection(userId);
		var wsId = Guid.NewGuid();
		var ws = Substitute.For<WebSocket>();
		ws.State.Returns(WebSocketState.Closed);
		connection.AddConnection(wsId, ws);

		// Act
		await connection.RemoveConnectionAsync(wsId);

		// Assert
		await ws.DidNotReceive().CloseAsync(Arg.Any<WebSocketCloseStatus>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
		ws.Received(1).Dispose();
		Assert.DoesNotContain(wsId, connection.GetWebSocketIds());
	}

	[Fact]
	public async Task SendMessageAsync_ShouldSendMessage_WhenWebSocketIsOpen()
	{
		// Arrange
		var userId = "user6";
		var connection = new UserConnection(userId);
		var wsId = Guid.NewGuid();
		var ws = Substitute.For<WebSocket>();
		ws.State.Returns(WebSocketState.Open);
		connection.AddConnection(wsId, ws);
		var message = "Hello";

		// Act
		await connection.SendMessageAsync(wsId, message);

		// Assert
		await ws.Received(1).SendAsync(
			Arg.Is<ArraySegment<byte>>(b => Encoding.UTF8.GetString(b) == message),
			WebSocketMessageType.Text,
			true,
			CancellationToken.None
		);
	}

	[Fact]
	public async Task SendMessageAsync_ShouldNotSend_WhenMessageIsNullOrEmpty()
	{
		// Arrange
		var userId = "user7";
		var connection = new UserConnection(userId);
		var wsId = Guid.NewGuid();
		var ws = Substitute.For<WebSocket>();
		ws.State.Returns(WebSocketState.Open);
		connection.AddConnection(wsId, ws);

		// Act
		await connection.SendMessageAsync(wsId, null!);
		await connection.SendMessageAsync(wsId, "");

		// Assert
		await ws.DidNotReceive().SendAsync(
			Arg.Any<ArraySegment<byte>>(),
			Arg.Any<WebSocketMessageType>(),
			Arg.Any<bool>(),
			Arg.Any<CancellationToken>()
		);
	}

	[Fact]
	public async Task RemoveConnectionAsync_ShouldHandleException_WhenWebSocketThrows()
	{
		// Arrange
		var userId = "user_exception";
		var connection = new UserConnection(userId);
		var wsId = Guid.NewGuid();
		var ws = Substitute.For<WebSocket>();
		ws.State.Returns(WebSocketState.Open);
		ws.CloseAsync(
			Arg.Any<WebSocketCloseStatus>(),
			Arg.Any<string>(),
			Arg.Any<CancellationToken>()
		).Returns<Task>(x => throw new Exception("Close failed"));
		connection.AddConnection(wsId, ws);

		// Act
		var exception = await Record.ExceptionAsync(() => connection.RemoveConnectionAsync(wsId));

		// Assert
		Assert.Null(exception); // 應被 try-catch 吃掉
		ws.Received(1).Dispose();
		Assert.DoesNotContain(wsId, connection.GetWebSocketIds());
	}

	[Fact]
	public async Task SendMessageAsync_ShouldHandleException_WhenSendAsyncThrows()
	{
		// Arrange
		var userId = "user_exception2";
		var connection = new UserConnection(userId);
		var wsId = Guid.NewGuid();
		var ws = Substitute.For<WebSocket>();
		ws.State.Returns(WebSocketState.Open);
		ws.SendAsync(
			Arg.Any<ArraySegment<byte>>(),
			Arg.Any<WebSocketMessageType>(),
			Arg.Any<bool>(),
			Arg.Any<CancellationToken>()
		).Returns<Task>(x => throw new Exception("Send failed"));
		connection.AddConnection(wsId, ws);

		// Act
		var exception = await Record.ExceptionAsync(() => connection.SendMessageAsync(wsId, "test"));

		// Assert
		Assert.Null(exception); // 應被 try-catch 吃掉
	}
}