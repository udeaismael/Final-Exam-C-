using System.ComponentModel.DataAnnotations;

namespace RentalAPI.Web.ViewModels;

public class LoginViewModel
{
    [Required, EmailAddress, Display(Name = "Correo electrónico")]
    public string Email { get; set; } = "";

    [Required, DataType(DataType.Password), Display(Name = "Contraseña")]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required, Display(Name = "Nombres")]
    public string FirstName { get; set; } = "";

    [Required, Display(Name = "Apellidos")]
    public string LastName { get; set; } = "";

    [Required, EmailAddress, Display(Name = "Correo electrónico")]
    public string Email { get; set; } = "";

    [Required, DataType(DataType.Password), MinLength(6), Display(Name = "Contraseña")]
    public string Password { get; set; } = "";

    [Required, Display(Name = "Tipo de cuenta")]
    public string Role { get; set; } = "User"; // User | Owner
}
