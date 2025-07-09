namespace GHLearning.EasyWebSocket.Core.Users.Models;
public record UserMessage(
	UserMessageRisk Risk,
	UserMessageType Type,
	DateTimeOffset SendAt,
	string Message);
