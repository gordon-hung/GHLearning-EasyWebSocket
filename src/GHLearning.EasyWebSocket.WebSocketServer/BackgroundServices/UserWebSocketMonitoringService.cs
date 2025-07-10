using GHLearning.EasyWebSocket.Core.Users;

namespace GHLearning.EasyWebSocket.WebSocketServer.BackgroundServices;

public class UserWebSocketMonitoringService(IUserWebSocketService userWebSocket) : BackgroundService
{
	protected override Task ExecuteAsync(CancellationToken stoppingToken)
		=> userWebSocket.MonitorConnectionsAsync(stoppingToken);
}
