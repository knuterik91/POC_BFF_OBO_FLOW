using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;

namespace POC_OBO_FLOW.Server.Controllers;

[ApiController]
[Authorize]
[Route("bff/debug")]
public sealed class TokenDebugController(
    ITokenAcquisition tokenAcquisition,
    IConfiguration configuration,
    IHostEnvironment environment) : ControllerBase
{
    [HttpGet("access-token")]
    public async Task<IActionResult> GetAccessTokenDebugInfo()
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        var scopes = configuration.GetSection("DownstreamApi:Scopes")
            .GetChildren()
            .Select(scope => scope.Value)
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Select(scope => scope!)
            .ToArray();

        if (scopes.Length == 0)
        {
            return BadRequest("DownstreamApi:Scopes is not configured.");
        }

        var accessToken = await tokenAcquisition.GetAccessTokenForUserAsync(scopes);
        var parts = accessToken.Split('.');

        if (parts.Length < 2)
        {
            return Ok(new
            {
                warning = "Access token is not a readable JWT.",
                token = MaskToken(accessToken),
                tokenLength = accessToken.Length,
                scopes
            });
        }

        return Ok(new
        {
            warning = "Development-only endpoint. The raw access token is intentionally not returned.",
            token = MaskToken(accessToken),
            tokenLength = accessToken.Length,
            scopes,
            jwtHeader = DecodeJwtPart(parts[0]),
            jwtPayload = DecodeJwtPart(parts[1])
        });
    }

    private static string MaskToken(string token)
    {
        if (token.Length <= 24)
        {
            return "<token hidden>";
        }

        return $"{token[..12]}...{token[^12..]}";
    }

    private static JsonElement DecodeJwtPart(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');

        var json = Encoding.UTF8.GetString(Convert.FromBase64String(padded));
        using var document = JsonDocument.Parse(json);

        return document.RootElement.Clone();
    }
}
