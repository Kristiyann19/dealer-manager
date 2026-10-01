using DealerManager.Application.Dtos.Identity;
using DealerManager.Application.IService.Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace DealerManager.Controllers;

[ApiController, Route("api/auth"), Authorize(Policy = "AuthenticatedUser")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthController(IAuthenticationService authentication, IHostEnvironment environment) : ControllerBase
{
    [AllowAnonymous, HttpGet("csrf")]
    public IActionResult Csrf([FromServices] IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!, new CookieOptions {
            HttpOnly = false, Secure = Request.IsHttps || !(environment.IsDevelopment() || environment.IsEnvironment("Testing")),
            SameSite = SameSiteMode.Strict, Path = "/", IsEssential = true
        });
        return Ok(new { token = tokens.RequestToken });
    }
    [AllowAnonymous, HttpPost("register")]
    public async Task<ActionResult<CurrentUserDto>> Register(RegisterRequest request, CancellationToken token)
        => StatusCode(StatusCodes.Status201Created, await authentication.Register(request, token));
    [AllowAnonymous, HttpPost("login")]
    public async Task<ActionResult<CurrentUserDto>> Login(LoginRequest request, CancellationToken token)
    {
        var user = await authentication.Login(request, token);
        return user == null ? Unauthorized(new { message = "Invalid credentials." }) : Ok(user);
    }
    [HttpPost("logout")]
    public async Task<IActionResult> Logout() { await authentication.Logout(); return NoContent(); }
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserDto>> Me(CancellationToken token)
    {
        var user = await authentication.Me(token);
        return user == null ? Unauthorized() : Ok(user);
    }
}
