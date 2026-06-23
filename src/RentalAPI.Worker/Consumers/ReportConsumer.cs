using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RentalAPI.Worker.Consumers;

public class ReportConsumer(IConfiguration config, ILogger<ReportConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:Host"] ?? "localhost",
            UserName = config["RabbitMQ:Username"] ?? "guest",
            Password = config["RabbitMQ:Password"] ?? "guest"
        };

        await using var conn    = await factory.CreateConnectionAsync(ct);
        await using var channel = await conn.CreateChannelAsync(cancellationToken: ct);
        await channel.QueueDeclareAsync("report-generation", durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await channel.BasicQosAsync(0, 1, false, ct);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var body    = Encoding.UTF8.GetString(ea.Body.ToArray());
                var payload = JsonSerializer.Deserialize<ReportPayload>(body)!;

                logger.LogInformation("Report generation requested: {Type} by user {UserId}", payload.ReportType, payload.RequestedBy);
                // TODO: generate and store/send report
                await channel.BasicAckAsync(ea.DeliveryTag, false, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing report");
                await channel.BasicNackAsync(ea.DeliveryTag, false, true, ct);
            }
        };

        await channel.BasicConsumeAsync("report-generation", autoAck: false, consumer, ct);
        await Task.Delay(Timeout.Infinite, ct);
    }
}

public record ReportPayload(string ReportType, Guid RequestedBy, DateTime? From, DateTime? To);
