using System.ComponentModel.DataAnnotations;

public class TokenModel
{
    [Required]
    public string? Jwt { get; set; }

    [Required]
    public string? RefreshToken { get; set; }

    public TokenModel() {}

    public TokenModel(string jwt, string refreshToken)
    {
        Jwt = jwt;
        RefreshToken = refreshToken;
    }
}