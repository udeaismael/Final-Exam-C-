using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace RentalAPI.Infrastructure.Messaging;

public class RabbitMqPublisher : IAsyncDisposable
{
    private readonly IConnection _conn;
    private readonly IChannel    _channel;

    public static async Task<RabbitMqPublisher> CreateAsync(IConfiguration config)
    {
        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:Host"] ?? "localhost",
            UserName = config["RabbitMQ:Username"] ?? "guest",
            Password = config["RabbitMQ:Password"] ?? "guest"
        };
        var conn    = await factory.CreateConnectionAsync();
        var channel = await conn.CreateChannelAsync();
        foreach (var q in new[] { "notifications", "kyc-processing", "report-generation" })
            await channel.QueueDeclareAsync(q, durable: true, exclusive: false, autoDelete: false);
        return new RabbitMqPublisher(conn, channel);
    }

    private RabbitMqPublisher(IConnection conn, IChannel channel)
    {
        _conn    = conn;
        _channel = channel;
    }

    public async Task PublishAsync<T>(string queue, T message)
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var props = new BasicProperties { Persistent = true };
        await _channel.BasicPublishAsync("", queue, false, props, body);
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.DisposeAsync();
        await _conn.DisposeAsync();
    }
}
