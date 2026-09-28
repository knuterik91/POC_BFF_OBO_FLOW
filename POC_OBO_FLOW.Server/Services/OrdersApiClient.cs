using Microsoft.Identity.Abstractions;
using POC_OBO_FLOW.Server.Models;

namespace POC_OBO_FLOW.Server.Services;

public sealed class OrdersApiClient(IDownstreamApi downstreamApi)
{
    public async Task<OrdersApiResponse?> GetOrdersForCurrentUserAsync(CancellationToken cancellationToken)
    {
        return await downstreamApi.GetForUserAsync<OrdersApiResponse>(
            "OrdersApi",
            options => options.RelativePath = "api/orders",
            cancellationToken: cancellationToken);
    }
}
