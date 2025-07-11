using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GHLearning.EasyWebSocket.Infrastructure.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;

namespace GHLearning.EasyWebSocket.InfrastructureTests.Authentication;

public class TokenProviderTests
{
	private static TokenOptions CreateTokenOptions() => new()
	{
		Issuer = "TestIssuer",
		Audience = "TestAudience",
		SecurityKey = "YRTW01NNZJGJGIWW6I7E2MJV4LN0SEzi",
		ExpirationMinutes = 60
	};

	private static IOptions<TokenOptions> CreateOptions() => Options.Create(CreateTokenOptions());

	[Fact]
	public void GenerateToken_ShouldReturnValidJwt()
	{
		// Arrange
		var timeProvider = Substitute.For<TimeProvider>();
		var jwtHandler = new JwtSecurityTokenHandler();
		var provider = new TokenProvider(CreateOptions(), timeProvider, jwtHandler);

		var account = "testuser";

		// Act
		var token = provider.GenerateToken(account);

		// Assert
		Assert.False(string.IsNullOrEmpty(token));
		var jwt = jwtHandler.ReadJwtToken(token);
		Assert.Equal("TestIssuer", jwt.Issuer);
		Assert.Equal("TestAudience", jwt.Audiences.First());
		Assert.Equal(account, jwt.Claims.First(c => c.Type == ClaimTypes.Name).Value);
	}

	[Fact]
	public void ValidateToken_ShouldReturnAccount_WhenTokenIsValidAndNotExpired()
	{
		// Arrange
		var timeProvider = Substitute.For<TimeProvider>();
		var jwtHandler = new JwtSecurityTokenHandler();
		var provider = new TokenProvider(CreateOptions(), timeProvider, jwtHandler);

		var account = "testuser";
		var token = provider.GenerateToken(account);

		var jwt = jwtHandler.ReadJwtToken(token);
		var exp = jwt.Payload.Expiration ?? 0;
		timeProvider.GetUtcNow().Returns(DateTimeOffset.FromUnixTimeSeconds(exp - 10));

		// Act
		var result = provider.ValidateToken(token);

		// Assert
		Assert.Equal(account, result);
	}

	[Fact]
	public void ValidateToken_ShouldReturnEmpty_WhenTokenIsExpired()
	{
		// Arrange
		var timeProvider = Substitute.For<TimeProvider>();
		var jwtHandler = new JwtSecurityTokenHandler();
		var provider = new TokenProvider(CreateOptions(), timeProvider, jwtHandler);

		var account = "testuser";
		var token = provider.GenerateToken(account);

		var jwt = jwtHandler.ReadJwtToken(token);
		var exp = jwt.Payload.Expiration ?? 0;
		timeProvider.GetUtcNow().Returns(DateTimeOffset.FromUnixTimeSeconds(exp + 10));

		// Act
		var result = provider.ValidateToken(token);

		// Assert
		Assert.Equal(string.Empty, result);
	}

	[Fact]
	public void ValidateToken_ShouldReturnEmpty_WhenTokenIsMalformed()
	{
		// Arrange
		var timeProvider = Substitute.For<TimeProvider>();
		var jwtHandler = new JwtSecurityTokenHandler();
		var provider = new TokenProvider(CreateOptions(), timeProvider, jwtHandler);

		var invalidToken = "not.a.valid.token";

		// Act
		var result = Assert.Throws<SecurityTokenMalformedException>(() => provider.ValidateToken(invalidToken));

		// Assert
		Assert.Contains("JWT is not well formed", result.Message);
	}
}