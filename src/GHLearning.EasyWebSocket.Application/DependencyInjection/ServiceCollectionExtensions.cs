using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace GHLearning.EasyWebSocket.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddApplication(this IServiceCollection services)
		=> services.AddMediatR(config => config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

}
