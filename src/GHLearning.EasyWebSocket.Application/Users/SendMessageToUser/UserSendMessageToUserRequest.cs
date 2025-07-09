using GHLearning.EasyWebSocket.Core.Users.Models;
using MediatR;

namespace GHLearning.EasyWebSocket.Application.Users.SendMessageToUser;
public record UserSendMessageToUserRequest(
	string UserId,
	UserMessageRisk Risk,
	string Message) : IRequest;
