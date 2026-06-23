using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalAPI.Domain.Entities;
using RentalAPI.Infrastructure.Data;
using RentalAPI.Infrastructure.Messaging;

namespace RentalAPI.Api.Controllers;

[ApiController]
[Route("api/kyc")]
[Authorize]
public class KycController(AppDbContext db, RabbitMqPublisher mq, IWebHostEnvironment env) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ── POST /api/kyc/upload ─────────────────────────────────────────────────
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file provided.");

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            return BadRequest("Only JPEG, PNG, and WebP images are allowed.");

        if (file.Length > 10 * 1024 * 1024)
            return BadRequest("File size must be under 10MB.");

        // Check if already approved
        var existing = await db.KycValidations.FirstOrDefaultAsync(k => k.UserId == UserId);
        if (existing?.Status == "Approved")
            return Conflict("KYC already approved.");

        // Save image temporarily with a random name (no extension leaking user info)
        var tempDir  = Path.Combine(env.ContentRootPath, "temp_kyc");
        Directory.CreateDirectory(tempDir);
        var tempPath = Path.Combine(tempDir, $"{Guid.NewGuid()}.tmp");

        await using (var stream = System.IO.File.Create(tempPath))
            await file.CopyToAsync(stream);

        // Create/update KYC record
        if (existing is null)
        {
            existing = new KycValidation
            {
                UserId       = UserId,
                Status       = "Pending",
                TempImagePath = tempPath
            };
            db.KycValidations.Add(existing);
        }
        else
        {
            existing.Status        = "Pending";
            existing.TempImagePath = tempPath;
            existing.ProcessedAt   = null;
        }

        await db.SaveChangesAsync();

        // Enqueue for async OCR processing
        await mq.PublishAsync("kyc-processing", new { KycId = existing.Id, TempImagePath = tempPath });

        return Accepted(new { existing.Id, existing.Status, Message = "KYC submitted for processing." });
    }

    // ── GET /api/kyc/status ──────────────────────────────────────────────────
    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        var kyc = await db.KycValidations.FirstOrDefaultAsync(k => k.UserId == UserId);
        if (kyc is null) return NotFound("No KYC submission found.");
        return Ok(new { kyc.Status, kyc.ProcessedAt, kyc.RejectionReason });
    }
}
