# GHLearning-EasyWebSocket

[![.NET](https://github.com/gordon-hung/GHLearning-EasyWebSocket/actions/workflows/dotnet.yml/badge.svg)](https://github.com/gordon-hung/GHLearning-EasyWebSocket/actions/workflows/dotnet.yml)

[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/gordon-hung/GHLearning-EasyWebSocket)

This article explains how to get started with WebSockets in ASP.NET Core. [WebSocket](https://wikipedia.org/wiki/WebSocket) ([RFC 6455](https://tools.ietf.org/html/rfc6455)) is a protocol that enables two-way persistent communication channels over TCP connections. It's used in apps that benefit from fast, real-time communication, such as chat, dashboard, and game apps.Gordon Hung Learning Easy WebSocket

## Configure the middleware

Add the WebSockets middleware in `Program.cs`:

```
app.UseWebSockets();
```

The following settings can be configured:

* [KeepAliveInterval](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.builder.websocketoptions.keepaliveinterval) - How frequently to send "ping" frames to the client to ensure proxies keep the connection open. The default is two minutes.
* [AllowedOrigins](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.builder.websocketoptions.allowedorigins) - A list of allowed Origin header values for WebSocket requests. By default, all origins are allowed. For more information, see [WebSocket origin restriction](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/websockets?view=aspnetcore-9.0#websocket-origin-restriction) in this article.

```
var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
};

app.UseWebSockets(webSocketOptions);
```
