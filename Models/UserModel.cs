using System.ComponentModel.DataAnnotations;

public class UserModel
{
    [Required]
    public string? Username = null;
    
    [Required]
    public string? Password = null;
}