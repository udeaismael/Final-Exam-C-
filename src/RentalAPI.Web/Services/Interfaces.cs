using RentalAPI.Web.Models;

namespace RentalAPI.Web.Services;

public interface IAuthService
{
    Task<(bool Success, TokenResponseDto? Token, string? Error)> LoginAsync(LoginRequestDto req);
    Task<(bool Success, string? Error)> RegisterAsync(RegisterRequestDto req);
}

public interface IPropertyService
{
    Task<List<PropertySearchItemDto>> SearchAsync(string? city, DateTime? checkIn, DateTime? checkOut);
    Task<PropertyDto?> GetAsync(Guid id);
    Task<List<PropertyDto>> GetMyPropertiesAsync(Guid ownerId);
    Task<(bool Success, string? Error, PropertyDto? Created)> CreateAsync(CreatePropertyRequestDto req);
    Task<(bool Success, string? Error)> UpdateAsync(Guid id, CreatePropertyRequestDto req);
    Task<bool> DeleteAsync(Guid id);
}

public interface IReservationService
{
    Task<(bool Success, string? Error)> CreateAsync(CreateReservationRequestDto req);
    Task<List<ReservationDto>> GetMineAsync();
}

public interface IWishlistService
{
    Task<List<WishlistItemDto>> GetAllAsync();
    Task<bool> AddAsync(Guid propertyId);
    Task<bool> RemoveAsync(Guid propertyId);
}

public interface IKycService
{
    Task<(bool Success, string? Error)> UploadAsync(Stream fileStream, string fileName, string contentType);
    Task<KycStatusDto?> GetStatusAsync();
}

public interface IDashboardService
{
    Task<ViewModels.DashboardViewModel> GetOwnerDashboardAsync(Guid ownerId);
}

public interface IReportService
{
    Task<(bool Success, byte[]? File, string? FileName)> ExportReservationsAsync(DateTime? from, DateTime? to);
}
