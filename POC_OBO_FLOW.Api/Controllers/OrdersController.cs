using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Abstractions;
using POC_OBO_FLOW.Api.Models;

namespace POC_OBO_FLOW.Api.Controllers;

[ApiController]
[Authorize(Policy = "access_as_user")]
[Authorize(Policy = "Les")]
[Route("api/orders")]
public sealed class OrdersController(IDownstreamApi downstreamApi) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<OrdersApiResponse>> GetOrders(CancellationToken cancellationToken)
    {
        var orders = new[]
        {
            new OrderDto("ORD-10001", "Reference order line A", "CUS-2042", "Ready for review", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))),
            new OrderDto("ORD-10002", "Reference order line B", "CUS-3188", "Pending approval", DateOnly.FromDateTime(DateTime.UtcNow))
        };

        var claims = User.Claims
            .Select(claim => new ApiClaimDto(claim.Type, claim.Value))
            .OrderBy(claim => claim.Type)
            .ToArray();

        var graphMe = await downstreamApi.GetForUserAsync<GraphMeDto>(
            "Graph",
            options => options.RelativePath = "me",
            cancellationToken: cancellationToken);

        return Ok(new OrdersApiResponse(orders, claims, graphMe));
    }
}
