using GHLearning.EasyWebSocket.Core.Users;
using Microsoft.Extensions.Hosting;

namespace GHLearning.EasyWebSocket.Infrastructure.Users;
public class UserWebSocketMonitoringService(IUserWebSocketService userWebSocket) : BackgroundService
{
	protected override Task ExecuteAsync(CancellationToken stoppingToken)
		=> userWebSocket.MonitorConnectionsAsync(stoppingToken);
}
