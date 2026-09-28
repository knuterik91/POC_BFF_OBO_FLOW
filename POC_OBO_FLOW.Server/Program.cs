using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Identity.Web;
using POC_OBO_FLOW.Server.Services;

var builder = WebApplication.CreateBuilder(args);
var requiredLoginRole = builder.Configuration["Authorization:RequiredLoginRole"] ?? "Tilgang";

builder.Services
    .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddDownstreamApi("OrdersApi", builder.Configuration.GetSection("DownstreamApi"))
    .AddInMemoryTokenCaches();

builder.Services.PostConfigure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    var previousOnTokenValidated = options.Events.OnTokenValidated;

    options.Events.OnTokenValidated = async context =>
    {
        if (previousOnTokenValidated is not null)
        {
            await previousOnTokenValidated(context);
        }

        if (context.Principal is null || !HasRole(context.Principal, requiredLoginRole))
        {
            context.Fail($"User must have the '{requiredLoginRole}' app role to sign in to the BFF.");
        }
    };
});

builder.Services.Configure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.Cookie.Name = "__Host-poc-obo-bff";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "__Host-poc-obo-xsrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthorizeFilter("Tilgang"));
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Tilgang", policy => policy
        .RequireAuthenticatedUser()
        .RequireAssertion(context => HasRole(context.User, requiredLoginRole)));
});

builder.Services.AddScoped<OrdersApiClient>();

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseDefaultFiles();
app.MapStaticAssets();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.Unspecified,
    Secure = CookieSecurePolicy.Always
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("/index.html");

app.Run();

static bool HasRole(ClaimsPrincipal principal, string roleName)
{
    return principal.IsInRole(roleName) ||
        principal.HasClaim(claim =>
            (claim.Type == "roles" || claim.Type == ClaimTypes.Role) &&
            string.Equals(claim.Value, roleName, StringComparison.Ordinal));
}
