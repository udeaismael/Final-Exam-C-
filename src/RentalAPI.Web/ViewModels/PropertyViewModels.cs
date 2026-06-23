using System.ComponentModel.DataAnnotations;
using RentalAPI.Web.Models;

namespace RentalAPI.Web.ViewModels;

public class PropertySearchViewModel
{
    public string? City { get; set; }
    public DateTime? CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
    public List<PropertySearchItemDto> Results { get; set; } = [];
}

public class PropertyFormViewModel
{
    public Guid? Id { get; set; }

    [Required, Display(Name = "Título")]
    public string Title { get; set; } = "";

    [Required, Display(Name = "Descripción")]
    public string Description { get; set; } = "";

    [Required, Display(Name = "Ciudad")]
    public string City { get; set; } = "";

    [Required, Display(Name = "Dirección")]
    public string Address { get; set; } = "";

    [Required, Range(1, 100000), Display(Name = "Precio por noche (USD)")]
    public decimal PricePerNight { get; set; }

    [Range(1, 50), Display(Name = "Huéspedes máx.")]
    public int MaxGuests { get; set; } = 1;

    [Range(0, 20), Display(Name = "Habitaciones")]
    public int Bedrooms { get; set; }

    [Range(0, 20), Display(Name = "Baños")]
    public int Bathrooms { get; set; }
}
