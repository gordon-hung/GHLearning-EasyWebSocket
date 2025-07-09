using GHLearning.EasyWebSocket.Core.Users;
using GHLearning.EasyWebSocket.Infrastructure.Users;
using Microsoft.Extensions.DependencyInjection;

namespace GHLearning.EasyWebSocket.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddInfrastructure(
		this IServiceCollection services)
		=> services
		.AddSingleton(TimeProvider.System)
		// 註冊用戶 WebSocket 服務
		.AddSingleton<IUserWebSocketService, UserWebSocketService>()
		// 註冊 WebSocket 監控服務
		.AddHostedService<UserWebSocketMonitoringService>();
}
