using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalAPI.Domain.Entities;
using RentalAPI.Infrastructure.Data;
using RentalAPI.Infrastructure.Messaging;

namespace RentalAPI.Api.Controllers;

[ApiController]
[Route("api/reservations")]
[Authorize]
public class ReservationsController(AppDbContext db, RabbitMqPublisher mq) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<IActionResult> Create(CreateReservationRequest req)
    {
        var kyc = await db.KycValidations
            .FirstOrDefaultAsync(k => k.UserId == UserId);

        if (kyc is null || kyc.Status != "Approved")
            return BadRequest("KYC validation required before making reservations.");

        if (req.CheckIn.Date >= req.CheckOut.Date)
            return BadRequest("CheckOut must be after CheckIn.");

        // ─────────────────────────────────────────────
        // FIX CRÍTICO: normalizar a UTC + Kind explícito
        // ─────────────────────────────────────────────

        var checkIn = DateTime.SpecifyKind(
            req.CheckIn.Date.AddHours(14),
            DateTimeKind.Utc);

        var checkOut = DateTime.SpecifyKind(
            req.CheckOut.Date.AddHours(12),
            DateTimeKind.Utc);

        // Overlap check
        var overlap = await db.Reservations.AnyAsync(r =>
            r.PropertyId == req.PropertyId &&
            r.Status != "Cancelled" &&
            r.CheckIn < checkOut &&
            r.CheckOut > checkIn);

        if (overlap)
            return Conflict("Property is not available for the selected dates.");

        var property = await db.Properties
            .FirstOrDefaultAsync(p => p.Id == req.PropertyId && p.IsActive);

        if (property is null)
            return NotFound("Property not found.");

        var nights = (checkOut.Date - checkIn.Date).Days;
        var totalPrice = property.PricePerNight * nights;

        var reservation = new Reservation
        {
            UserId = UserId,
            PropertyId = req.PropertyId,

            // ya vienen UTC correctos
            CheckIn = checkIn,
            CheckOut = checkOut,

            TotalPrice = totalPrice,
            Status = "Confirmed",

            // también asegurado UTC (buena práctica explícita)
            CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
        };

        db.Reservations.Add(reservation);
        await db.SaveChangesAsync();

        await mq.PublishAsync("notifications", new
        {
            UserId,
            Type = "Email",
            Subject = "Reservation confirmed",
            Body = $"Your reservation at {property.Title} is confirmed. Check-in: {checkIn:yyyy-MM-dd HH:mm} UTC"
        });

        return CreatedAtAction(nameof(Get), new { id = reservation.Id }, reservation);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var r = await db.Reservations
            .Include(x => x.Property)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId);

        return r is null ? NotFound() : Ok(r);
    }

    [HttpGet]
    public async Task<IActionResult> GetMine()
    {
        var list = await db.Reservations
            .Where(r => r.UserId == UserId)
            .Include(r => r.Property)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(list);
    }
}

public record CreateReservationRequest(Guid PropertyId, DateTime CheckIn, DateTime CheckOut);