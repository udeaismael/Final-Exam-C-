using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
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

public class KycOcrService(IConfiguration config, ILogger<KycOcrService> logger)
{
    private readonly string? _azureEndpoint = config["Azure:DocumentIntelligence:Endpoint"];
    private readonly string? _azureApiKey = config["Azure:DocumentIntelligence:ApiKey"];

    public async Task<KycOcrResult> ProcessAsync(string imagePath)
    {
        if (!string.IsNullOrEmpty(_azureEndpoint) &&
            !string.IsNullOrEmpty(_azureApiKey))
        {
            try
            {
                return await ProcessWithAzureAsync(imagePath);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    "Azure OCR failed, falling back to Tesseract: {Error}",
                    ex.Message);
            }
        }

        return ProcessWithTesseract(imagePath);
    }

    // Azure Document Intelligence
    private async Task<KycOcrResult> ProcessWithAzureAsync(string imagePath)
    {
        using var client = new HttpClient();

        client.DefaultRequestHeaders.Add(
            "Ocp-Apim-Subscription-Key",
            _azureApiKey);

        var imageBytes = await File.ReadAllBytesAsync(imagePath);

        using var content = new ByteArrayContent(imageBytes);

        content.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

        var analyzeUrl =
            $"{_azureEndpoint}/documentintelligence/documentModels/prebuilt-idDocument:analyze?api-version=2024-02-29-preview";

        var response = await client.PostAsync(analyzeUrl, content);

        response.EnsureSuccessStatusCode();

        var operationLocation =
            response.Headers.GetValues("Operation-Location").First();

        for (int i = 0; i < 30; i++)
        {
            await Task.Delay(2000);

            var pollResponse = await client.GetAsync(operationLocation);

            pollResponse.EnsureSuccessStatusCode();

            var json =
                await pollResponse.Content.ReadAsStringAsync();

            var poll =
                System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);

            var status = poll.GetProperty("status").GetString();

            if (status == "succeeded")
            {
                var fields = poll
                    .GetProperty("analyzeResult")
                    .GetProperty("documents")[0]
                    .GetProperty("fields");

                return new KycOcrResult
                {
                    FirstName = GetField(fields, "FirstName"),
                    LastName = GetField(fields, "LastName"),
                    DocumentNumber = GetField(fields, "DocumentNumber"),
                    BirthDate = DateTime.TryParse(
                        GetField(fields, "DateOfBirth"),
                        out var dob)
                        ? dob
                        : null,
                    Success = true
                };
            }

            if (status == "failed")
            {
                throw new Exception("Azure analysis failed.");
            }
        }

        throw new Exception("Azure analysis timed out.");
    }

    private static string? GetField(
        System.Text.Json.JsonElement fields,
        string name)
    {
        if (fields.TryGetProperty(name, out var field) &&
            field.TryGetProperty("content", out var content))
        {
            return content.GetString();
        }

        return null;
    }

    // Tesseract fallback
    private KycOcrResult ProcessWithTesseract(string imagePath)
    {
        try
        {
            using var engine = new Tesseract.TesseractEngine(
                "/usr/share/tesseract-ocr/5/tessdata",
                "spa+eng",
                Tesseract.EngineMode.Default);

            using var img = Tesseract.Pix.LoadFromFile(imagePath);
            using var page = engine.Process(img);

            var text = page.GetText();

            logger.LogDebug(
                "Tesseract raw output: {Text}",
                text);

            return ParseRawText(text);
        }
        catch (Exception ex)
        {
            logger.LogError(
                "Tesseract error: {Error}",
                ex.Message);

            return new KycOcrResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    private static KycOcrResult ParseRawText(string text)
    {
        var result = new KycOcrResult();

        var docMatch =
            Regex.Match(text, @"\b\d{8,10}\b");

        if (docMatch.Success)
            result.DocumentNumber = docMatch.Value;

        var dateMatch =
            Regex.Match(
                text,
                @"\b(\d{2}[/\-]\d{2}[/\-]\d{4}|\d{4}[/\-]\d{2}[/\-]\d{2})\b");

        if (dateMatch.Success &&
            DateTime.TryParse(dateMatch.Value, out var dob))
        {
            result.BirthDate = dob;
        }

        var apellidosMatch =
            Regex.Match(
                text,
                @"(?i)apellidos?\s*:?\s*([A-ZÁÉÍÓÚÑ ]+)",
                RegexOptions.IgnoreCase);

        if (apellidosMatch.Success)
            result.LastName = apellidosMatch.Groups[1].Value.Trim();

        var nombresMatch =
            Regex.Match(
                text,
                @"(?i)nombres?\s*:?\s*([A-ZÁÉÍÓÚÑ ]+)",
                RegexOptions.IgnoreCase);

        if (nombresMatch.Success)
            result.FirstName = nombresMatch.Groups[1].Value.Trim();

        result.Success = result.DocumentNumber is not null;

        return result;
    }
}