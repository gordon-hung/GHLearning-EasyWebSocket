using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;
using System.Net.Mime;
using System.Text.Json.Serialization;
using GHLearning.EasyWebSocket.Infrastructure.DependencyInjection;
using GHLearning.EasyWebSocket.Application.DependencyInjection;
using GHLearning.EasyWebSocket.Core.Users;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Add services to the container.
builder.Services
	.AddRouting(options => options.LowercaseUrls = true)
	.AddControllers(options =>
	{
		options.Filters.Add(new ProducesAttribute(MediaTypeNames.Application.Json));
		options.Filters.Add(new ConsumesAttribute(MediaTypeNames.Application.Json));
	})
	.AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddInfrastructure();
builder.Services.AddApplication();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "OpenAPI V1"));// swagger/
	app.UseReDoc(options => options.SpecUrl("/openapi/v1.json"));//api-docs/
	app.MapScalarApiReference();//scalar/v1
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseWebSockets(new WebSocketOptions
{
	KeepAliveInterval = TimeSpan.FromSeconds(30) // 每 30 秒保持活躍
});

app.Map("/ws", context =>
{
	var userWebSocket = context.RequestServices.GetRequiredService<IUserWebSocketService>();
	return userWebSocket.HandleWebSocketConnectionAsync(context);
});
app.Run();
