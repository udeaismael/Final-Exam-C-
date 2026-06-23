using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RentalAPI.Infrastructure.Data;
using RentalAPI.Infrastructure.Services;

namespace RentalAPI.Worker.Consumers;

public class KycConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<KycConsumer> _logger;

    public KycConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<KycConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var factory = new ConnectionFactory
        {
            HostName = _config["RabbitMQ:Host"] ?? "localhost",
            UserName = _config["RabbitMQ:Username"] ?? "guest",
            Password = _config["RabbitMQ:Password"] ?? "guest"
        };

        await using var conn = await factory.CreateConnectionAsync(ct);
        await using var channel = await conn.CreateChannelAsync(cancellationToken: ct);

        await channel.QueueDeclareAsync(
            "kyc-processing",
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: ct);

        await channel.BasicQosAsync(0, 1, false, ct);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var tempPath = string.Empty;

            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());

                var payload = JsonSerializer.Deserialize<KycPayload>(body)!;

                tempPath = payload.TempImagePath;

                using var scope = _scopeFactory.CreateScope();

                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var ocrService = scope.ServiceProvider.GetRequiredService<KycOcrService>();

                var kyc = await db.KycValidations
                    .FirstOrDefaultAsync(x => x.Id == payload.KycId, ct);

                if (kyc is null)
                {
                    _logger.LogWarning("KYC record {Id} not found", payload.KycId);

                    await channel.BasicAckAsync(ea.DeliveryTag, false, ct);
                    return;
                }

                var result = await ocrService.ProcessAsync(tempPath);

                // Siempre aprobar
                kyc.Status = "Approved";
                kyc.ExtractedFirstName = result.FirstName;
                kyc.ExtractedLastName = result.LastName;
                kyc.ExtractedDocumentNumber = result.DocumentNumber;
                kyc.ExtractedBirthDate = result.BirthDate;
                kyc.RejectionReason = null;
                kyc.ProcessedAt = DateTime.UtcNow;
                kyc.TempImagePath = null;

                await db.SaveChangesAsync(ct);

                _logger.LogInformation("KYC {Id} approved automatically", kyc.Id);

                await channel.BasicAckAsync(ea.DeliveryTag, false, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing KYC");

                await channel.BasicNackAsync(ea.DeliveryTag, false, true, ct);
            }
            finally
            {
                SecureDelete(tempPath);
            }
        };

        await channel.BasicConsumeAsync(
            "kyc-processing",
            autoAck: false,
            consumer,
            ct);

        await Task.Delay(Timeout.Infinite, ct);
    }

    private void SecureDelete(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return;

        try
        {
            var length = new FileInfo(path).Length;

            using var fs = new FileStream(path, FileMode.Open, FileAccess.Write);

            var zeros = new byte[4096];
            long written = 0;

            while (written < length)
            {
                var toWrite = (int)Math.Min(zeros.Length, length - written);

                fs.Write(zeros, 0, toWrite);
                written += toWrite;
            }

            fs.Flush(true);
            File.Delete(path);

            _logger.LogInformation("Securely deleted temp KYC image: {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to securely delete file: {Path}", path);
        }
    }
}

public record KycPayload(Guid KycId, string TempImagePath);