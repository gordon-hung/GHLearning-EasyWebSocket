using GHLearning.EasyWebSocket.Core.Users.Models;
using MediatR;

namespace GHLearning.EasyWebSocket.Application.Users.SendMessageToAll;
public record UserSendMessageToAllRequest(
	UserMessageRisk Risk,
	string Message) : IRequest;
