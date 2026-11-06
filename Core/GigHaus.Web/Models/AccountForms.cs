using System.ComponentModel.DataAnnotations;

namespace GigHaus.Web.Models;

public sealed class LoginForm
{
    [Required(ErrorMessage = "Enter your email.")]
    [EmailAddress(ErrorMessage = "That doesn't look like an email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your password.")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}

public class VenueDetailsForm
{
    [Required(ErrorMessage = "What should we call you or your venue?")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Enter your street address.")]
    [StringLength(500)]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your city.")]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your state.")]
    [StringLength(50)]
    public string State { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your ZIP code.")]
    [StringLength(20)]
    public string ZipCode { get; set; } = string.Empty;

    [Range(1, 1000, ErrorMessage = "Enter how many guests fit, from 1 to 1000.")]
    public int MaxCapacity { get; set; } = 20;

    [StringLength(4000)]
    public string? VenueDescription { get; set; }
}

public sealed class HostSignupForm : VenueDetailsForm
{
    [Required(ErrorMessage = "Enter your email.")]
    [EmailAddress(ErrorMessage = "That doesn't look like an email address.")]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose a password.")]
    [StringLength(100, MinimumLength = 10, ErrorMessage = "Use at least 10 characters.")]
    public string Password { get; set; } = string.Empty;
}

public sealed class ForgotPasswordForm
{
    [Required(ErrorMessage = "Enter your email.")]
    [EmailAddress(ErrorMessage = "That doesn't look like an email address.")]
    public string Email { get; set; } = string.Empty;
}

public sealed class ResetPasswordForm
{
    [Required(ErrorMessage = "Choose a new password.")]
    [StringLength(100, MinimumLength = 10, ErrorMessage = "Use at least 10 characters.")]
    public string Password { get; set; } = string.Empty;

    [Compare(nameof(Password), ErrorMessage = "The passwords don't match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class ChangePasswordForm
{
    [Required(ErrorMessage = "Enter your current password.")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose a new password.")]
    [StringLength(100, MinimumLength = 10, ErrorMessage = "Use at least 10 characters.")]
    public string NewPassword { get; set; } = string.Empty;

    [Compare(nameof(NewPassword), ErrorMessage = "The passwords don't match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class StaffForm
{
    [Required(ErrorMessage = "Enter a name.")]
    [StringLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter an email.")]
    [EmailAddress(ErrorMessage = "That doesn't look like an email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose a temporary password.")]
    [StringLength(100, MinimumLength = 10, ErrorMessage = "Use at least 10 characters.")]
    public string Password { get; set; } = string.Empty;
}
