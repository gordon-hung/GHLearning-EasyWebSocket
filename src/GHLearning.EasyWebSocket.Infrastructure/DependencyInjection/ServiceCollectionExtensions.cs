using System.IdentityModel.Tokens.Jwt;
using GHLearning.EasyWebSocket.Core.Users;
using GHLearning.EasyWebSocket.Infrastructure.Authentication;
using GHLearning.EasyWebSocket.Infrastructure.Users;
using GHLearning.EasyWebSocket.SharedKernel.Abstractions.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace GHLearning.EasyWebSocket.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddInfrastructure(
		this IServiceCollection services,
		Action<TokenOptions, IServiceProvider> tokenOptions)
		=> services
		.AddOptions<TokenOptions>()
		.Configure(tokenOptions)
		.Services
		.AddSingleton(TimeProvider.System)
		// 註冊 JwtSecurityTokenHandler
		.AddSingleton(new JwtSecurityTokenHandler())
		// 註冊 JWT 安全令牌處理程序
		.AddSingleton<ITokenProvider, TokenProvider>()
		// 註冊多用戶的連線管理
		.AddSingleton<IUserConnectionManager, UserConnectionManager>()
		// 註冊WebSocket服務相關的操作
		.AddSingleton<IUserWebSocketService, UserWebSocketService>()
		// 註冊用戶接收消息處理器
		.AddSingleton<IUserReceiveMessagesHandler, UserReceiveMessagesHandler>();
}
