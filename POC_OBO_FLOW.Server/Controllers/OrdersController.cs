using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POC_OBO_FLOW.Server.Services;

namespace POC_OBO_FLOW.Server.Controllers;

[ApiController]
[Authorize]
[Route("bff/orders")]
public sealed class OrdersController(OrdersApiClient ordersApiClient) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetOrders(CancellationToken cancellationToken)
    {
        var result = await ordersApiClient.GetOrdersForCurrentUserAsync(cancellationToken);

        return Ok(result);
    }
}
