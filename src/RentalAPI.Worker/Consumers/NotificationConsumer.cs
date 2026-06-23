using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RentalAPI.Domain.Entities;
using RentalAPI.Infrastructure.Data;

namespace RentalAPI.Worker.Consumers;

public class NotificationConsumer(IServiceScopeFactory scopeFactory, IConfiguration config, ILogger<NotificationConsumer> logger)
    : BackgroundService
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
        await channel.QueueDeclareAsync("notifications", durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await channel.BasicQosAsync(0, 1, false, ct);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var body    = Encoding.UTF8.GetString(ea.Body.ToArray());
                var payload = JsonSerializer.Deserialize<NotificationPayload>(body)!;

                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                db.Notifications.Add(new Notification
                {
                    UserId  = payload.UserId,
                    Type    = payload.Type,
                    Subject = payload.Subject,
                    Body    = payload.Body,
                    IsSent  = true,
                    SentAt  = DateTime.UtcNow
                });
                await db.SaveChangesAsync(ct);

                // TODO: integrate real email/push provider here
                logger.LogInformation("Notification sent to user {UserId}: {Subject}", payload.UserId, payload.Subject);
                await channel.BasicAckAsync(ea.DeliveryTag, false, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing notification");
                await channel.BasicNackAsync(ea.DeliveryTag, false, true, ct);
            }
        };

        await channel.BasicConsumeAsync("notifications", autoAck: false, consumer, ct);
        await Task.Delay(Timeout.Infinite, ct);
    }
}

public record NotificationPayload(Guid UserId, string Type, string Subject, string Body);
