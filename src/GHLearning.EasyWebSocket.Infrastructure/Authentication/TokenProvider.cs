using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GHLearning.EasyWebSocket.SharedKernel.Abstractions.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GHLearning.EasyWebSocket.Infrastructure.Authentication;

internal sealed class TokenProvider(
	IOptions<TokenOptions> options,
	TimeProvider timeProvider,
	JwtSecurityTokenHandler jwtSecurityTokenHandler) : ITokenProvider
{
	private readonly TokenOptions _tokenOptions = options.Value;

	public string GenerateToken(string account)
	{
		var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_tokenOptions.SecurityKey));
		var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

		var claims = new List<Claim>
		{
			new(ClaimTypes.Name, account)
		};

		var token = new JwtSecurityToken(
			issuer: _tokenOptions.Issuer,  // 你自己的發行者名稱
			audience: _tokenOptions.Audience,  // 你自己的受眾名稱
			claims: claims,  // 在這裡將 claims 傳遞給 token
			expires: DateTime.Now.AddMinutes(_tokenOptions.ExpirationMinutes),  // 設定過期時間
			signingCredentials: credentials  // 使用簽名憑證
		);

		return jwtSecurityTokenHandler.WriteToken(token);  // 生成 JWT 並返回
	}

	public string ValidateToken(string token)
	{
		var jsonToken = jwtSecurityTokenHandler.ReadToken(token) as JwtSecurityToken;
		var expirationDate = jsonToken?.Payload?.Expiration;

		if (expirationDate != null && expirationDate.Value > timeProvider.GetUtcNow().ToUnixTimeSeconds())
		{
			return jsonToken?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? string.Empty;
		}

		return string.Empty;  // 如果驗證失敗，返回空字符串
	}
}
