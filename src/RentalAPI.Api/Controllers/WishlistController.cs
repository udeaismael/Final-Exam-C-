using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalAPI.Domain.Entities;
using RentalAPI.Infrastructure.Data;

namespace RentalAPI.Api.Controllers;

[ApiController]
[Route("api/wishlist")]
[Authorize]
public class WishlistController(AppDbContext db) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ── GET /api/wishlist ─────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await db.WishlistItems
            .Where(w => w.UserId == UserId)
            .Include(w => w.Property)
            .Select(w => new { w.Id, w.PropertyId, w.Property.Title, w.Property.City, w.Property.PricePerNight, w.CreatedAt })
            .ToListAsync();
        return Ok(items);
    }

    // ── POST /api/wishlist/{propertyId} ──────────────────────────────────────
    [HttpPost("{propertyId:guid}")]
    public async Task<IActionResult> Add(Guid propertyId)
    {
        var exists = await db.WishlistItems.AnyAsync(w => w.UserId == UserId && w.PropertyId == propertyId);
        if (exists) return Conflict("Already in wishlist.");

        var propertyExists = await db.Properties.AnyAsync(p => p.Id == propertyId && p.IsActive);
        if (!propertyExists) return NotFound("Property not found.");

        db.WishlistItems.Add(new WishlistItem { UserId = UserId, PropertyId = propertyId });
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── DELETE /api/wishlist/{propertyId} ────────────────────────────────────
    [HttpDelete("{propertyId:guid}")]
    public async Task<IActionResult> Remove(Guid propertyId)
    {
        var item = await db.WishlistItems.FirstOrDefaultAsync(w => w.UserId == UserId && w.PropertyId == propertyId);
        if (item is null) return NotFound();
        db.WishlistItems.Remove(item);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
