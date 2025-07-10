namespace GHLearning.EasyWebSocket.Infrastructure.Authentication;
public record TokenOptions
{
	public string Issuer { get; init; } = default!;
	public string Audience { get; init; } = default!;
	public string SecurityKey { get; init; } = default!;
	public int ExpirationMinutes { get; init; }
}
