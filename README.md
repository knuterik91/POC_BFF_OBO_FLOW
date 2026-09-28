# ASP.NET Core 10 BFF + OBO reference project

This solution demonstrates a server-side Backend-for-Frontend pattern with Microsoft Entra ID, OpenID Connect sign-in, HttpOnly cookies, and On-Behalf-Of calls from the BFF to a protected API.

The browser never receives or stores access tokens.

## Projects

```text
POC_OBO_FLOW/
├─ POC_OBO_FLOW.Server/       # BFF + static React host
├─ POC_OBO_FLOW.Api/          # Protected downstream API
└─ poc_obo_flow.client/       # React client, built into BFF wwwroot
```

## Local URLs

| Component | URL |
| --- | --- |
| BFF | `https://localhost:5001` |
| API | `https://localhost:7001` |

Use the BFF URL in the browser:

```text
https://localhost:5001
```

## Microsoft Entra ID configuration

The existing Entra setup is expected to contain:

### BFF app registration

Redirect URI:

```text
https://localhost:5001/signin-oidc
```

Front-channel logout URL:

```text
https://localhost:5001/signout-callback-oidc
```

The BFF uses a client secret stored in user-secrets:

```powershell
dotnet user-secrets set "AzureAd:ClientSecret" "<secret>" --project .\POC_OBO_FLOW.Server\POC_OBO_FLOW.Server.csproj
```

The BFF app registration must also define an app role named:

```text
Tilgang
```

Assign the signed-in user, or a group containing the user, to the `Tilgang` role on the BFF Enterprise Application. The BFF rejects sign-in if this role is missing.

### API app registration

Exposed delegated scope:

```text
api://<API_CLIENT_ID>/access_as_user
```

The BFF app registration must have delegated permission to `access_as_user`, and admin consent must be granted.

The API app registration must also define an app role named:

```text
Les
```

Assign the user, or a group containing the user, to the `Les` role on the API Enterprise Application. The API requires this role on the OBO access token.

## BFF configuration

`POC_OBO_FLOW.Server/appsettings.json` contains:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<tenant-id>",
    "ClientId": "<bff-client-id>",
    "ClientSecret": "sett_i_kv",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  },
  "DownstreamApi": {
    "BaseUrl": "https://localhost:7001/",
    "Scopes": [
      "api://<api-client-id>/access_as_user"
    ]
  },
  "Authorization": {
    "RequiredLoginRole": "Tilgang"
  }
}
```

The BFF uses:

- `AddMicrosoftIdentityWebApp(...)`
- `EnableTokenAcquisitionToCallDownstreamApi()`
- `AddDownstreamApi(...)`
- `IDownstreamApi`
- Secure HttpOnly cookies
- Antiforgery protection for state-changing BFF endpoints
- App role `Tilgang` for BFF sign-in and BFF endpoint access

## API configuration

`POC_OBO_FLOW.Api/appsettings.json` contains:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<tenant-id>",
    "ClientId": "<api-client-id>",
    "Audience": "api://<api-client-id>"
  },
  "Authorization": {
    "RequiredApiRole": "Les"
  }
}
```

The API uses:

- `AddMicrosoftIdentityWebApi(...)`
- JWT Bearer validation
- Policy `access_as_user`, which requires the delegated `scp` claim to contain `access_as_user`
- Policy `Les`, which requires the API app role claim `roles = Les`

## Implemented flow

1. React loads from the BFF at `https://localhost:5001`.
2. User clicks login.
3. BFF starts OpenID Connect sign-in with Entra ID.
4. Entra redirects back to `/signin-oidc`.
5. BFF validates that the user has the BFF app role `Tilgang`.
6. BFF creates a secure HttpOnly cookie.
7. React calls `/bff/orders` with only the cookie.
8. BFF uses `IDownstreamApi` to acquire an OBO token for `access_as_user`.
9. BFF calls `https://localhost:7001/api/orders`.
10. API validates the JWT access token and checks `access_as_user` plus API app role `Les`.
11. API returns sample orders and the JWT claims it received.

## Authorization model

This version requires app roles in addition to authentication and delegated API scope.

BFF:

```csharp
[Authorize(Policy = "Tilgang")]
```

The `Tilgang` role is checked during the OIDC login callback and also through a global BFF authorization policy. A user without `roles = Tilgang` cannot sign in to the BFF.

API:

```csharp
[Authorize(Policy = "access_as_user")]
[Authorize(Policy = "Les")]
```

The API requires both:

- `scp` contains `access_as_user`
- `roles` contains `Les`

That means the demo validates:

- Interactive login
- BFF app-role authorization with `Tilgang`
- BFF cookie session
- OBO token acquisition
- API JWT Bearer validation
- Delegated scope authorization
- API app-role authorization with `Les`

## Run locally

Build the React client into the BFF static web root:

```powershell
npm run build --prefix .\poc_obo_flow.client
```

Start the API:

```powershell
dotnet run --project .\POC_OBO_FLOW.Api\POC_OBO_FLOW.Api.csproj --launch-profile https
```

Start the BFF:

```powershell
dotnet run --project .\POC_OBO_FLOW.Server\POC_OBO_FLOW.Server.csproj --launch-profile https
```

Open:

```text
https://localhost:5001
```

## Notes

- Do not use SPA authentication.
- Do not expose access tokens to React.
- The React app is a static UI served by the BFF.
- The BFF is the public web entry point.
- The API independently validates JWT access tokens and scope claims.
