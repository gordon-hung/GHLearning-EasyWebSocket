using System.Buffers.Text;
using System.Net;
using GHLearning.EasyWebSocket.Core.Users;
using GHLearning.EasyWebSocket.Core.Users.Models;
using GHLearning.EasyWebSocket.Infrastructure.Users;
using GHLearning.EasyWebSocket.SharedKernel.Abstractions.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using NSubstitute;

namespace GHLearning.EasyWebSocket.InfrastructureTests.Users;
public class UserConnectionManagerTests
{
	private readonly ILogger<UserConnectionManager> _logger = NullLogger<UserConnectionManager>.Instance;
	private readonly ITokenProvider _tokenProvider = Substitute.For<ITokenProvider>();
	private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
	private readonly System.Diagnostics.ActivitySource _activitySource = new("test");
	private readonly UserConnectionManager _manager;

	public UserConnectionManagerTests()
	{
		_manager = new UserConnectionManager(_logger, _tokenProvider, _timeProvider, _activitySource);
	}

	[Fact]
	public async Task HandleWebSocketConnectionAsync_ShouldRejectNonWebSocketRequest()
	{
		var context = Substitute.For<HttpContext>();
		context.WebSockets.IsWebSocketRequest.Returns(false);
		context.Connection.RemoteIpAddress.Returns(IPAddress.Loopback);

		await _manager.HandleWebSocketConnectionAsync(context);

		Assert.Equal((int)HttpStatusCode.UpgradeRequired, context.Response.StatusCode);
	}

	[Fact]
	public async Task HandleWebSocketConnectionAsync_ShouldRejectMissingToken()
	{
		var context = Substitute.For<HttpContext>();
		context.WebSockets.IsWebSocketRequest.Returns(true);
		context.Connection.RemoteIpAddress.Returns(IPAddress.Loopback);

		await _manager.HandleWebSocketConnectionAsync(context);

		Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
	}

	[Fact]
	public async Task HandleWebSocketConnectionAsync_ShouldRejectInvalidToken()
	{
		var context = Substitute.For<HttpContext>();
		context.WebSockets.IsWebSocketRequest.Returns(true);
		context.Request.Query["token"].Returns(new StringValues("badtoken"));
		_tokenProvider.ValidateToken("badtoken").Returns("");
		context.Connection.RemoteIpAddress.Returns(IPAddress.Loopback);

		await _manager.HandleWebSocketConnectionAsync(context);

		Assert.Equal((int)HttpStatusCode.Unauthorized, context.Response.StatusCode);
	}

	[Fact]
	public async Task SendMessageToAllAsync_ShouldSendToAllConnections()
	{
		var userConnection = Substitute.For<IUserConnection>();
		var userId = "user1";
		var wsId = Guid.NewGuid();
		userConnection.GetWebSocketIds().Returns([wsId]);
		_timeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow);

		// 直接注入到 _userConnections
		var field = typeof(UserConnectionManager).GetField("_userConnections", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		var dict = new System.Collections.Concurrent.ConcurrentDictionary<string, IUserConnection>();
		dict[userId] = userConnection;
		field!.SetValue(_manager, dict);

		await _manager.SendMessageToAllAsync(UserMessageRisk.Low, "msg");

		await userConnection.Received(1).SendMessageAsync(wsId, Arg.Any<string>());
	}

	[Fact]
	public async Task ForceDisconnectUserAsync_ShouldRemoveAllConnections()
	{
		var userConnection = Substitute.For<IUserConnection>();
		var userId = "user2";
		var wsId = Guid.NewGuid();
		userConnection.GetWebSocketIds().Returns([wsId]);

		var field = typeof(UserConnectionManager).GetField("_userConnections", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		var dict = new System.Collections.Concurrent.ConcurrentDictionary<string, IUserConnection>();
		dict[userId] = userConnection;
		field!.SetValue(_manager, dict);

		await _manager.ForceDisconnectUserAsync(userId);

		await userConnection.Received(1).RemoveConnectionAsync(wsId);
	}

	[Fact]
	public async Task ForceDisconnectUserAsync_ShouldLogWarning_WhenUserNotFound()
	{
		await _manager.ForceDisconnectUserAsync("notfound");
	}

	[Fact]
	public async Task SendMessageToUserAsync_ShouldSendToUserConnections()
	{
		var userConnection = Substitute.For<IUserConnection>();
		var userId = "user3";
		var wsId = Guid.NewGuid();
		userConnection.GetWebSocketIds().Returns([wsId]);
		_timeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow);

		var field = typeof(UserConnectionManager).GetField("_userConnections", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		var dict = new System.Collections.Concurrent.ConcurrentDictionary<string, IUserConnection>();
		dict[userId] = userConnection;
		field!.SetValue(_manager, dict);

		await _manager.SendMessageToUserAsync(userId, UserMessageRisk.High, "private");

		await userConnection.Received(1).SendMessageAsync(wsId, Arg.Any<string>());
	}

	[Fact]
	public async Task SendMessageToUserAsync_ShouldLogWarning_WhenUserNotFound()
	{
		await _manager.SendMessageToUserAsync("notfound", UserMessageRisk.Low, "msg");
	}

	[Fact]
	public async Task ForceDisconnectAllUsersAsync_ShouldCallForceDisconnectUserAsync()
	{
		var userConnection = Substitute.For<IUserConnection>();
		var userId = "user4";
		var wsId = Guid.NewGuid();
		userConnection.GetWebSocketIds().Returns([wsId]);

		var field = typeof(UserConnectionManager).GetField("_userConnections", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		var dict = new System.Collections.Concurrent.ConcurrentDictionary<string, IUserConnection>();
		dict[userId] = userConnection;
		field!.SetValue(_manager, dict);

		await _manager.ForceDisconnectAllUsersAsync();

		await userConnection.Received(1).RemoveConnectionAsync(wsId);
	}

	[Fact]
	public async Task SendMessageToAllAsync_ShouldCatchException_WhenSendFails()
	{
		var userConnection = Substitute.For<IUserConnection>();
		var userId = "user_exception";
		var wsId = Guid.NewGuid();
		userConnection.GetWebSocketIds().Returns([wsId]);
		userConnection.SendMessageAsync(wsId, Arg.Any<string>())
			.Returns<Task>(x => throw new Exception("Send failed"));
		_timeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow);

		// 直接注入到 _userConnections
		var field = typeof(UserConnectionManager).GetField("_userConnections", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		var dict = (System.Collections.Concurrent.ConcurrentDictionary<string, IUserConnection>)field!.GetValue(_manager)!;
		dict[userId] = userConnection;

		var ex = await Record.ExceptionAsync(() => _manager.SendMessageToAllAsync(UserMessageRisk.Low, "msg"));
		Assert.Null(ex); // 應被 try-catch 吃掉
	}

	[Fact]
	public async Task SendMessageToUserAsync_ShouldCatchException_WhenSendFails()
	{
		var userConnection = Substitute.For<IUserConnection>();
		var userId = "user_exception2";
		var wsId = Guid.NewGuid();
		userConnection.GetWebSocketIds().Returns([wsId]);
		userConnection.SendMessageAsync(wsId, Arg.Any<string>())
			.Returns<Task>(x => throw new Exception("Send failed"));
		_timeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow);

		var field = typeof(UserConnectionManager).GetField("_userConnections", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		var dict = (System.Collections.Concurrent.ConcurrentDictionary<string, IUserConnection>)field!.GetValue(_manager)!;
		dict[userId] = userConnection;

		var ex = await Record.ExceptionAsync(() => _manager.SendMessageToUserAsync(userId, UserMessageRisk.Low, "msg"));
		Assert.Null(ex);
	}

	[Fact]
	public async Task ForceDisconnectUserAsync_ShouldCatchException_WhenRemoveFails()
	{
		var userConnection = Substitute.For<IUserConnection>();
		var userId = "user_exception3";
		var wsId = Guid.NewGuid();
		userConnection.GetWebSocketIds().Returns([wsId]);
		userConnection.RemoveConnectionAsync(wsId)
			.Returns<Task>(x => throw new Exception("Remove failed"));

		var field = typeof(UserConnectionManager).GetField("_userConnections", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		var dict = (System.Collections.Concurrent.ConcurrentDictionary<string, IUserConnection>)field!.GetValue(_manager)!;
		dict[userId] = userConnection;

		var ex = await Record.ExceptionAsync(() => _manager.ForceDisconnectUserAsync(userId));
		Assert.Null(ex);
	}

	[Fact]
	public async Task ForceDisconnectAllUsersAsync_ShouldCatchException_WhenForceDisconnectUserFails()
	{
		var userConnection = Substitute.For<IUserConnection>();
		var userId = "user_exception4";
		var wsId = Guid.NewGuid();
		userConnection.GetWebSocketIds().Returns([wsId]);
		userConnection.RemoveConnectionAsync(wsId)
			.Returns<Task>(x => throw new Exception("Remove failed"));

		var field = typeof(UserConnectionManager).GetField("_userConnections", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		var dict = (System.Collections.Concurrent.ConcurrentDictionary<string, IUserConnection>)field!.GetValue(_manager)!;
		dict[userId] = userConnection;

		var ex = await Record.ExceptionAsync(() => _manager.ForceDisconnectAllUsersAsync());
		Assert.Null(ex);
	}
}