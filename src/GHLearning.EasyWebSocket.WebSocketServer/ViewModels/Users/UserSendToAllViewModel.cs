using System.ComponentModel.DataAnnotations;
using GHLearning.EasyWebSocket.Core.Users.Models;

namespace GHLearning.EasyWebSocket.WebSocketServer.ViewModels.Users;

public record UserSendToAllViewModel
{
	[Required]
	public UserMessageRisk Risk { get; init; } = UserMessageRisk.Low;
	[Required]
	public string Message { get; init; } = default!;
}
