using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace POC_OBO_FLOW.Server.Controllers;

[ApiController]
[Authorize]
[Route("bff/user")]
public sealed class UserController : ControllerBase
{
    [HttpGet]
    public IActionResult GetCurrentUser()
    {
        var claims = User.Claims
            .Select(claim => new
            {
                claim.Type,
                claim.Value
            })
            .OrderBy(claim => claim.Type)
            .ToArray();

        return Ok(new
        {
            isAuthenticated = User.Identity?.IsAuthenticated == true,
            name = User.Identity?.Name,
            claims
        });
    }
}
