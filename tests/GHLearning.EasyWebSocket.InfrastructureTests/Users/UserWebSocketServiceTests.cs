using GHLearning.EasyWebSocket.Core.Users;
using GHLearning.EasyWebSocket.Core.Users.Models;
using GHLearning.EasyWebSocket.Infrastructure.Users;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace GHLearning.EasyWebSocket.InfrastructureTests.Users;
public class UserWebSocketServiceTests
{
	private readonly IUserConnectionManager _connectionManager = Substitute.For<IUserConnectionManager>();
	private readonly UserWebSocketService _service;

	public UserWebSocketServiceTests()
	{
		_service = new UserWebSocketService(_connectionManager);
	}

	[Fact]
	public async Task HandleWebSocketConnectionAsync_ShouldCallConnectionManager()
	{
		var context = Substitute.For<HttpContext>();
		await _service.HandleWebSocketConnectionAsync(context);
		await _connectionManager.Received(1).HandleWebSocketConnectionAsync(context);
	}

	[Fact]
	public async Task SendMessageToAllAsync_ShouldCallConnectionManager()
	{
		await _service.SendMessageToAllAsync(UserMessageRisk.Low, "msg");
		await _connectionManager.Received(1).SendMessageToAllAsync(UserMessageRisk.Low, "msg", Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ForceDisconnectUserAsync_ShouldCallConnectionManager()
	{
		await _service.ForceDisconnectUserAsync("user1");
		await _connectionManager.Received(1).ForceDisconnectUserAsync("user1", Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ForceDisconnectAllUsersAsync_ShouldCallConnectionManager()
	{
		await _service.ForceDisconnectAllUsersAsync();
		await _connectionManager.Received(1).ForceDisconnectAllUsersAsync(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task SendMessageToUserAsync_ShouldCallConnectionManager()
	{
		await _service.SendMessageToUserAsync("user2", UserMessageRisk.High, "private");
		await _connectionManager.Received(1).SendMessageToUserAsync("user2", UserMessageRisk.High, "private", Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task MonitorConnectionsAsync_ShouldCallConnectionManager()
	{
		await _service.MonitorConnectionsAsync();
		await _connectionManager.Received(1).MonitorConnectionsAsync(Arg.Any<CancellationToken>());
	}
}
