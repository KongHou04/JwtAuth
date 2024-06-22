using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("account")]
public class AccountController(EmailSender emailSender, UserManager<AppUser> userManager) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = new AppUser { UserName = model.Email, Email = model.Email };
        var result = await userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok();
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        if (string.IsNullOrEmpty(email))
            return BadRequest("Email is required.");

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
            return BadRequest("User not found.");

        // Generate password reset token
        var token = await userManager.GeneratePasswordResetTokenAsync(user);

        // Build password reset link
        var resetLink = Url.Action("reset-password", "account", new { email, token }, Request.Scheme);
        try
        {
            await emailSender.SendEmailAsync(email, $"Reset your password", "<div>You can change your password by clicking <a href=\"" + resetLink + "\">here</a></div>");
            return Ok("Password reset link has been sent to your email.");
        }
        catch 
        {
            return BadRequest("Cannot send link to your email");
        }
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await userManager.FindByEmailAsync(model.Email);
        if (user == null)
            return BadRequest("User not found.");

        var result = await userManager.ResetPasswordAsync(user, model.Token!, model.NewPassword!);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok("Password has been reset successfully.");
    }

}