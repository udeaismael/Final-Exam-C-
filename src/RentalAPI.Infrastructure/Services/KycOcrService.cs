using Microsoft.Extensions.Logging;

namespace RentalAPI.Infrastructure.Services;

public class KycOcrResult
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DocumentNumber { get; set; }
    public DateTime? BirthDate { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}

public class KycOcrService
{
    private readonly ILogger<KycOcrService> _logger;

    public KycOcrService(ILogger<KycOcrService> logger)
    {
        _logger = logger;
    }

    public async Task<KycOcrResult> ProcessAsync(string imagePath)
    {
        try
        {
            if (!File.Exists(imagePath))
            {
                _logger.LogWarning("KYC image not found: {Path}", imagePath);

                return new KycOcrResult
                {
                    Success = false,
                    Error = "File not found"
                };
            }

            var fileInfo = new FileInfo(imagePath);

            if (fileInfo.Length <= 0)
            {
                _logger.LogWarning("KYC image is empty: {Path}", imagePath);

                return new KycOcrResult
                {
                    Success = false,
                    Error = "Empty file"
                };
            }

            _logger.LogInformation(
                "KYC image validated successfully: {Path}",
                imagePath);

            return await Task.FromResult(new KycOcrResult
            {
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing KYC document");

            return new KycOcrResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }
}