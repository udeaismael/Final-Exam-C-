using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalAPI.Domain.Entities;
using RentalAPI.Infrastructure.Data;

namespace RentalAPI.Api.Controllers;

[ApiController]
[Route("api/properties")]
public class PropertiesController(AppDbContext db) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ── GET /api/properties?city=&checkIn=&checkOut= ─────────────────────────
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? city,
        [FromQuery] DateTime? checkIn,
        [FromQuery] DateTime? checkOut)
    {
        var q = db.Properties.Where(p => p.IsActive);

        if (!string.IsNullOrEmpty(city))
            q = q.Where(p => EF.Functions.ILike(p.City, $"%{city}%"));

        // Exclude properties that have overlapping active reservations
        // Overlap condition: existing.CheckIn < requested.CheckOut AND existing.CheckOut > requested.CheckIn
        if (checkIn.HasValue && checkOut.HasValue)
        {
            var ci = checkIn.Value.Date;
            var co = checkOut.Value.Date;
            q = q.Where(p => !p.Reservations.Any(r =>
                r.Status != "Cancelled" &&
                r.CheckIn.Date  < co &&
                r.CheckOut.Date > ci));
        }

        var results = await q.Select(p => new
        {
            p.Id, p.Title, p.City, p.PricePerNight,
            p.Bedrooms, p.Bathrooms, p.MaxGuests
        }).ToListAsync();

        return Ok(results);
    }

    // ── GET /api/properties/{id} ─────────────────────────────────────────────
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var p = await db.Properties
            .Where(x => x.Id == id && x.IsActive)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Description,
                x.City,
                x.Address,
                x.PricePerNight,
                x.MaxGuests,
                x.Bedrooms,
                x.Bathrooms,
                Owner = new
                {
                    x.OwnerId,
                    x.Owner.FirstName,
                    x.Owner.LastName,
                    x.Owner.Email
                }
            })
            .FirstOrDefaultAsync();

        return p is null ? NotFound() : Ok(p);
    }

    // ── POST /api/properties ─────────────────────────────────────────────────
    [HttpPost, Authorize(Roles = "Owner,Admin")]
    public async Task<IActionResult> Create(CreatePropertyRequest req)
    {
        var property = new Property
        {
            OwnerId      = UserId,
            Title        = req.Title,
            Description  = req.Description,
            City         = req.City,
            Address      = req.Address,
            PricePerNight = req.PricePerNight,
            MaxGuests    = req.MaxGuests,
            Bedrooms     = req.Bedrooms,
            Bathrooms    = req.Bathrooms
        };
        db.Properties.Add(property);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = property.Id }, property);
    }

    // ── PUT /api/properties/{id} ─────────────────────────────────────────────
    [HttpPut("{id:guid}"), Authorize(Roles = "Owner,Admin")]
    public async Task<IActionResult> Update(Guid id, CreatePropertyRequest req)
    {
        var p = await db.Properties.FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == UserId);
        if (p is null) return NotFound();

        p.Title         = req.Title;
        p.Description   = req.Description;
        p.City          = req.City;
        p.Address       = req.Address;
        p.PricePerNight = req.PricePerNight;
        p.MaxGuests     = req.MaxGuests;
        p.Bedrooms      = req.Bedrooms;
        p.Bathrooms     = req.Bathrooms;

        await db.SaveChangesAsync();
        return Ok(p);
    }

    // ── DELETE /api/properties/{id} ──────────────────────────────────────────
    [HttpDelete("{id:guid}"), Authorize(Roles = "Owner,Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var p = await db.Properties.FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == UserId);
        if (p is null) return NotFound();
        p.IsActive = false; // soft delete
        await db.SaveChangesAsync();
        return NoContent();
    }
}

public record CreatePropertyRequest(
    string Title, string Description, string City, string Address,
    decimal PricePerNight, int MaxGuests, int Bedrooms, int Bathrooms);
