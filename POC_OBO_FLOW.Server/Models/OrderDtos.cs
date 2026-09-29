namespace POC_OBO_FLOW.Server.Models;

public sealed record OrderDto(
    string Id,
    string Description,
    string CustomerReference,
    string Status,
    DateOnly OrderedOn);

public sealed record ApiClaimDto(string Type, string Value);

public sealed record GraphMeDto(
    string? Id,
    string? DisplayName,
    string? UserPrincipalName,
    string? Mail);

public sealed record OrdersApiResponse(
    IReadOnlyList<OrderDto> Orders,
    IReadOnlyList<ApiClaimDto> ApiClaims,
    GraphMeDto? GraphMe);
