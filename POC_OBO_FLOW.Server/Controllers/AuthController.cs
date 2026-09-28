using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace POC_OBO_FLOW.Server.Controllers;

[ApiController]
public sealed class AuthController(IAntiforgery antiforgery, IConfiguration configuration) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("bff/login")]
    public IActionResult Login([FromQuery] string? returnUrl = "/")
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(SanitizeLocalReturnUrl(returnUrl));
        }

        return Challenge(new AuthenticationProperties
        {
            RedirectUri = SanitizeLocalReturnUrl(returnUrl)
        }, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [Authorize]
    [HttpPost("bff/logout")]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        var frontendBaseUrl = configuration["Frontend:BaseUrl"] ?? "/";

        return SignOut(new AuthenticationProperties
        {
            RedirectUri = frontendBaseUrl
        }, CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [Authorize]
    [HttpGet("bff/csrf")]
    public IActionResult Csrf()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);

        return Ok(new
        {
            token = tokens.RequestToken,
            headerName = "X-CSRF-TOKEN",
            formFieldName = tokens.FormFieldName
        });
    }

    private string SanitizeLocalReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl))
        {
            return "/";
        }

        return returnUrl;
    }
}
