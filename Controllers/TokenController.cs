using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

[ApiController]
[Route("token")]
public class TokenController(IConfiguration configuration, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager) : ControllerBase
{
    [HttpPost("get")]
    public async Task<IActionResult> GetAccessToken([FromBody]UserModel userModel)
    {
        if (!ModelState.IsValid)
            return BadRequest();

        var user = await userManager.FindByNameAsync(userModel.Username!);
        if (user == null)
            return BadRequest();

        var result = await signInManager.CheckPasswordSignInAsync(user, userModel.Password!, false);
        if (!result.Succeeded)
            return BadRequest();

        // Generate jwt
        var token = await GenerateToken(user);

        // Create refresh token
        var refreshToken = GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.Now.AddHours(16);
        await userManager.UpdateAsync(user);

        return Ok(new
        {
            token,
            refreshToken
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshJwt([FromBody]TokenModel tokenModel)
    {
        if (string.IsNullOrWhiteSpace(tokenModel.Jwt) || string.IsNullOrWhiteSpace(tokenModel.RefreshToken))
            return BadRequest();

        var principal = GetPrincipalFromExpiredToken(tokenModel.Jwt);
        if (principal == null)
            return BadRequest();

        var userId = principal?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return BadRequest();
            
        var user = await userManager.FindByIdAsync(userId);
        if (user == null || user.RefreshToken != tokenModel.RefreshToken || user.RefreshTokenExpiry <= DateTime.Now || await userManager.IsLockedOutAsync(user))
            return BadRequest();

        var newAccessToken = await GenerateToken(user);
        var newRefreshToken = GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiry = DateTime.Now.AddHours(16);
        await userManager.UpdateAsync(user);

        return Ok(new TokenModel(newAccessToken, newRefreshToken));
    }


    private async Task<string> GenerateToken(AppUser user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email!),
        };

        var roles = await userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(30),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }

    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = false,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidAudience = configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!))
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
        var jwtSecurityToken = securityToken as JwtSecurityToken;

        if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            return null;
        }

        return principal;
    }
}