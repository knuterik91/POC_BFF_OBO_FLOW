using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var requiredApiRole = builder.Configuration["Authorization:RequiredApiRole"] ?? "Les";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(options =>
    {
        builder.Configuration.Bind("AzureAd", options);

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtBearerDiagnostics");

                logger.LogWarning(context.Exception, "JWT authentication failed: {Message}", context.Exception.Message);

                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtBearerDiagnostics");

                logger.LogWarning(
                    "JWT challenge. Error={Error}; Description={ErrorDescription}",
                    context.Error,
                    context.ErrorDescription);

                return Task.CompletedTask;
            }
        };
    }, options => builder.Configuration.Bind("AzureAd", options))
    .EnableTokenAcquisitionToCallDownstreamApi(options => builder.Configuration.Bind("AzureAd", options))
    .AddInMemoryTokenCaches();

builder.Services.AddDownstreamApi("Graph", builder.Configuration.GetSection("Graph"));

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("access_as_user", policy => policy
        .RequireAuthenticatedUser()
        .RequireAssertion(context =>
            context.User.Claims
                .Where(claim => claim.Type is "scp" or "http://schemas.microsoft.com/identity/claims/scope")
                .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Contains("access_as_user")));

    options.AddPolicy("Les", policy => policy
        .RequireAuthenticatedUser()
        .RequireAssertion(context => HasRole(context.User, requiredApiRole)));
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static bool HasRole(ClaimsPrincipal principal, string roleName)
{
    return principal.IsInRole(roleName) ||
        principal.HasClaim(claim =>
            (claim.Type == "roles" || claim.Type == ClaimTypes.Role) &&
            string.Equals(claim.Value, roleName, StringComparison.Ordinal));
}
