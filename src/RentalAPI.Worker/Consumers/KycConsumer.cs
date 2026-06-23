using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RentalAPI.Infrastructure.Data;
using RentalAPI.Infrastructure.Services;

namespace RentalAPI.Worker.Consumers;

public class KycConsumer(IServiceScopeFactory scopeFactory, IConfiguration config, ILogger<KycConsumer> logger)
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
        await channel.QueueDeclareAsync("kyc-processing", durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await channel.BasicQosAsync(0, 1, false, ct);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var tempPath = "";
            try
            {
                var body    = Encoding.UTF8.GetString(ea.Body.ToArray());
                var payload = JsonSerializer.Deserialize<KycPayload>(body)!;
                tempPath    = payload.TempImagePath;

                using var scope  = scopeFactory.CreateScope();
                var db           = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var ocrService   = scope.ServiceProvider.GetRequiredService<KycOcrService>();

                var kyc = await db.KycValidations.FindAsync([payload.KycId], ct);
                if (kyc is null)
                {
                    logger.LogWarning("KYC record {Id} not found", payload.KycId);
                    await channel.BasicAckAsync(ea.DeliveryTag, false, ct);
                    return;
                }

                // Run OCR
                var result = await ocrService.ProcessAsync(tempPath);

                if (result.Success)
                {
                    kyc.Status                 = "Approved";
                    kyc.ExtractedFirstName     = result.FirstName;
                    kyc.ExtractedLastName      = result.LastName;
                    kyc.ExtractedDocumentNumber = result.DocumentNumber;
                    kyc.ExtractedBirthDate     = result.BirthDate;
                }
                else
                {
                    kyc.Status           = "Rejected";
                    kyc.RejectionReason  = result.Error ?? "Could not extract required data from document.";
                }

                kyc.ProcessedAt   = DateTime.UtcNow;
                kyc.TempImagePath = null; // clear path from DB
                await db.SaveChangesAsync(ct);

                logger.LogInformation("KYC {Id} processed → {Status}", kyc.Id, kyc.Status);
                await channel.BasicAckAsync(ea.DeliveryTag, false, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing KYC");
                await channel.BasicNackAsync(ea.DeliveryTag, false, true, ct);
            }
            finally
            {
                // Secure deletion: overwrite with zeros then delete
                SecureDelete(tempPath);
            }
        };

        await channel.BasicConsumeAsync("kyc-processing", autoAck: false, consumer, ct);
        await Task.Delay(Timeout.Infinite, ct);
    }

    private void SecureDelete(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
        try
        {
            var length = new FileInfo(path).Length;
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Write))
            {
                var zeros = new byte[4096];
                long written = 0;
                while (written < length)
                {
                    var toWrite = (int)Math.Min(zeros.Length, length - written);
                    fs.Write(zeros, 0, toWrite);
                    written += toWrite;
                }
                fs.Flush(true);
            }
            File.Delete(path);
            logger.LogInformation("Securely deleted temp KYC image: {Path}", path);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to securely delete file: {Path}", path);
        }
    }
}

public record KycPayload(Guid KycId, string TempImagePath);
