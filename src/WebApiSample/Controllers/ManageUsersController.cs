using AspNet.Identity.DynamoDB.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApiSample.Models;
using WebApiSample.ViewModels;

namespace WebApiSample.Controllers;

[Route("identity/users")]
public class ManageUsersController(
    UserManager<DynamoDbIdentityUser> userManager, 
    SignInManager<DynamoDbIdentityUser> signInManager,
    ILogger<ManageUsersController> logger,
    IConfiguration configuration) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost]
    [Route("register")]
    public async Task<IdentityResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        return await userManager.CreateAsync(new DynamoDbIdentityUser(model.Email, model.Username), model.Password);
    }

    [HttpPost]
    [AllowAnonymous]
    [Route("login")]
    public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
    {
        if (!ModelState.IsValid) return BadRequest(model);
        var user = await userManager.FindByEmailAsync(model.Email);

        var result = await signInManager.CheckPasswordSignInAsync(user,model.Password, false);
        if (!result.Succeeded) return BadRequest("Something went wrong");
        var token = GenerateJwtToken(user);
        return Ok(new { token });

    }

    private string GenerateJwtToken(DynamoDbIdentityUser user)
    {
        var handler = new JwtSecurityTokenHandler();

        var privateKey = Encoding.UTF8.GetBytes(configuration["Jwt:Key"]);

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(privateKey),
            SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            SigningCredentials = credentials,
            Expires = DateTime.UtcNow.AddHours(1),
            Subject = GenerateClaims(user)
        };

        var token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
    }

    private static ClaimsIdentity GenerateClaims(DynamoDbIdentityUser user)
    {
        var ci = new ClaimsIdentity();

        ci.AddClaim(new Claim(ClaimTypes.Email, user.Email));

        return ci;
    }
}