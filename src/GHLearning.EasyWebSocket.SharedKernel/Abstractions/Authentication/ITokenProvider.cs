namespace GHLearning.EasyWebSocket.SharedKernel.Abstractions.Authentication;

public interface ITokenProvider
{
	string GenerateToken(string account);

	string ValidateToken(string token);
}
