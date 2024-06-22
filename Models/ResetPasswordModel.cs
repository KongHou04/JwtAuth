using System.ComponentModel.DataAnnotations;

public class ResetPasswordModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string? NewPassword { get; set; }

    [Required]
    [StringLength(100)]
    [Compare(nameof(NewPassword))]
    public string? ConfirmPassword { get; set;}

    [Required]
    public string? Token { get; set; }
}