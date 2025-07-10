using GHLearning.EasyWebSocket.Application.Users.ForceDisconnectAllUsers;
using GHLearning.EasyWebSocket.Application.Users.ForceDisconnectUser;
using GHLearning.EasyWebSocket.Application.Users.Login;
using GHLearning.EasyWebSocket.Application.Users.SendMessageToAll;
using GHLearning.EasyWebSocket.Application.Users.SendMessageToUser;
using GHLearning.EasyWebSocket.WebSocketServer.ViewModels.Users;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GHLearning.EasyWebSocket.WebSocketServer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController(IMediator mediator) : ControllerBase
{
	/// <summary>
	/// Sends the message to user asynchronous.
	/// </summary>
	/// <param name="userId">The user identifier.</param>
	/// <param name="source">The source.</param>
	/// <returns></returns>
	[HttpPost("send/{userId}")]
	public Task SendMessageToUserAsync(string userId, [FromBody] UserSendToUserViewModel source)
		=> mediator.Send(
			request: new UserSendMessageToUserRequest(
				UserId: userId,
				Risk: source.Risk,
				Message: source.Message),
			cancellationToken: HttpContext.RequestAborted);

	/// <summary>
	/// Forces the disconnect user asynchronous.
	/// </summary>
	/// <param name="userId">The user identifier.</param>
	/// <returns></returns>
	[HttpDelete("force-disconnect/{userId}")]
	public Task ForceDisconnectUserAsync(string userId)
		=> mediator.Send(
			request: new UserForceDisconnectUserRequest(
				UserId: userId),
			cancellationToken: HttpContext.RequestAborted);

	/// <summary>
	/// Forces the disconnect all.
	/// </summary>
	/// <returns></returns>
	[HttpDelete("force-disconnect-all")]
	public Task ForceDisconnectAll()
		=> mediator.Send(
			request: new UserForceDisconnectAllUsersRequest(),
			cancellationToken: HttpContext.RequestAborted);

	/// <summary>
	/// Sends the message to all asynchronous.
	/// </summary>
	/// <param name="source">The source.</param>
	/// <returns></returns>
	[HttpPost("send-all")]
	public Task SendMessageToAllAsync([FromBody] UserSendToAllViewModel source)
		=> mediator.Send(
			request: new UserSendMessageToAllRequest(
				Risk: source.Risk,
				Message: source.Message),
			cancellationToken: HttpContext.RequestAborted);

	/// <summary>
	/// Users the login asynchronous.
	/// </summary>
	/// <param name="request">The request.</param>
	/// <returns></returns>
	[HttpPost("login")]
	public Task<UserLoginResponse> UserLoginAsync(
		[FromBody] UserLoginRequest request)
		=> mediator.Send(request, HttpContext.RequestAborted);
}
