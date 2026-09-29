# POC OBO Flow

Dette prosjektet demonstrerer en BFF-løsning med Microsoft Entra ID, Authorization Code Flow og ekte On-Behalf-Of flow mot Microsoft Graph.

## Prosjekter

- `poc_obo_flow.client` - React + TypeScript frontend.
- `POC_OBO_FLOW.Server` - ASP.NET Core BFF/webapp. Logger inn brukeren med OpenID Connect og Authorization Code Flow.
- `POC_OBO_FLOW.Api` - Beskyttet API. Validerer access token fra BFF og bruker OBO flow for å kalle Microsoft Graph `/me`.

## Flyt

```text
Browser
  -> BFF /bff/login

BFF
  -> Microsoft Entra ID
  <- id_token + access_token for POC_OBO_FLOW.Api

Browser
  -> BFF /bff/orders

BFF
  -> POC_OBO_FLOW.Api /api/orders
     Authorization: Bearer <access token for API>

POC_OBO_FLOW.Api
  -> Microsoft Entra ID OBO token exchange
  <- access_token for Microsoft Graph med User.Read

POC_OBO_FLOW.Api
  -> Microsoft Graph /me
  <- Graph user profile

Frontend
  <- orders + API claims + graphMe
```

Frontend mottar aldri access tokens direkte. Den bruker kun HttpOnly cookie-sesjonen mot BFF-en.

## Entra ID-konfigurasjon

Løsningen bruker to app registrations.

### BFF / Server app

Konfigurasjon i `POC_OBO_FLOW.Server/appsettings.json`:

- `AzureAd:ClientId`: BFF-appens client id.
- `AzureAd:TenantId`: tenant id.
- `DownstreamApi:Scopes`: API-scope som BFF ber om.

BFF-appregistreringen må ha delegated permission til API-et:

```text
api://<api-client-id>/access_as_user
```

Den må også ha en redirect URI som matcher:

```text
https://localhost:<server-port>/signin-oidc
```

### API app

Konfigurasjon i `POC_OBO_FLOW.Api/appsettings.json`:

- `AzureAd:ClientId`: API-appens client id.
- `AzureAd:Audience`: `api://<api-client-id>`.
- `Graph:Scopes`: `User.Read`.

API-appregistreringen må:

1. Eksponere et API-scope:

   ```text
   access_as_user
   ```

2. Ha delegated Microsoft Graph permission:

   ```text
   User.Read
   ```

3. Ha admin consent eller user consent for Graph-permissionen.

4. Ha en client credential for OBO token exchange:

   - client secret, eller
   - certificate.

Ikke legg ekte secrets i `appsettings.json`. Bruk user secrets, Key Vault eller miljøvariabler.

Eksempel for lokal utvikling:

```powershell
dotnet user-secrets set "AzureAd:ClientSecret" "<secret-verdi>" --project .\POC_OBO_FLOW.Api\POC_OBO_FLOW.Api.csproj
dotnet user-secrets set "AzureAd:ClientSecret" "<secret-verdi>" --project .\POC_OBO_FLOW.Server\POC_OBO_FLOW.Server.csproj
```

## Hva frontend viser

Etter innlogging kan brukeren trykke **Hent orders via BFF OBO**.

Frontend viser da:

- Orders returnert fra API-et.
- Graph `/me`-informasjon hentet av API-et via OBO:
  - display name
  - user principal name
  - mail
  - Graph id
- JWT-claims API-et mottok.

## Kjøring

Installer frontend-avhengigheter:

```powershell
cd .\poc_obo_flow.client
npm install
```

Kjør frontend i dev-modus:

```powershell
npm run dev
```

Bygg frontend:

```powershell
npm run build
```

Build-output legges i:

```text
POC_OBO_FLOW.Server/wwwroot
```

Bygg .NET-løsningen:

```powershell
dotnet build .\POC_OBO_FLOW.slnx
```

## Vanlige feil

### Graph `/me` feiler

Kontroller at API-appregistreringen har:

- delegated Microsoft Graph permission `User.Read`
- consent for `User.Read`
- gyldig client secret eller certificate

### BFF får ikke token til API

Kontroller at BFF-appregistreringen har delegated permission til API-scope:

```text
api://<api-client-id>/access_as_user
```

### Bruker blir avvist ved login

Server-prosjektet krever app role fra `Authorization:RequiredLoginRole`. API-et krever app role fra `Authorization:RequiredApiRole`.

Standardverdier i konfigurasjonen er:

- BFF: `Tilgang`
- API: `Les`
