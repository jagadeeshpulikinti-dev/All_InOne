# All In One — step-by-step setup

This solution uses `ALLINONE_Client` (Blazor WebAssembly), `ALL_IN_ONE_API` (ASP.NET Core API), `BL_ALL_IN_ONE` (business rules), `DL_ALL_IN_ONE` (EF Core and repositories), and `UL_ALL_IN_ONE` (contracts and JWT token issuer).

## 1. Configure SQL Server and JWT

The API is configured for SQL Server instance `localhost\JP`, database `AllInOneDb`, using Windows Integrated Authentication. The Windows account that runs the API must be allowed to connect to that SQL Server instance and create databases. On a new database, EF Core creates the schema; on an existing database, startup adds the `Role` column and `Apps` table without deleting existing users or notifications.

Set a private signing key before starting the API. Do not commit this key to source control:

```powershell
dotnet user-secrets init --project ALL_IN_ONE_API
$jwtKey = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
dotnet user-secrets set "Jwt:SigningKey" $jwtKey --project ALL_IN_ONE_API
Remove-Variable jwtKey
dotnet user-secrets set "AdminBootstrap:UserName" "jagadeesh" --project ALL_IN_ONE_API
$adminPassword = Read-Host "Choose a private admin password (12+ characters)" -AsSecureString
$adminPasswordText = [System.Net.NetworkCredential]::new("", $adminPassword).Password
dotnet user-secrets set "AdminBootstrap:Password" $adminPasswordText --project ALL_IN_ONE_API
Remove-Variable adminPassword, adminPasswordText
```

If no key is configured, Development mode now generates a temporary process-only key to let a fresh local checkout start. Tokens expire/become invalid after the API restarts in that fallback mode. Configure User Secrets above to keep a stable local key. Non-Development environments require an explicitly configured key.

`ALL_IN_ONE_API/appsettings.json` contains the local SQL connection string, JWT issuer/audience, and allowed browser origins. `ALLINONE_Client/wwwroot/appsettings.json` points the client at `https://localhost:7137`. Update these URLs and CORS origins together if launch profiles change.

## 2. Start the API

Trust the local ASP.NET development certificate if needed, then run:

```powershell
dotnet run --project ALL_IN_ONE_API --launch-profile https
```

Swagger is available at `https://localhost:7137/swagger`. Database creation happens during API startup. If startup reports a SQL connection error, check that SQL Server is running, the instance name is correct, and your Windows login has database creation rights.

## 3. Start the Blazor client

In a second terminal:

```powershell
dotnet run --project ALLINONE_Client --launch-profile https
```

The root URL `/` is the public AppHub directory. Sign in at `/login` or create an account at `/register`. Passwords are stored as PBKDF2 salted hashes. Signed-in users receive a 30-minute JWT; the client keeps it in memory and sends it on protected API calls. Reloading clears the token and requires a fresh sign-in.

The public directory loads app listings from `GET /api/apps`. Search and categories work on the public home page; choose at least two apps to compare. Listing details and external website links are available from app cards.

The first API startup adds the `Role` column and `Apps` table to the previous schema and inserts starter listings if the table is empty. Notifications at `/notifications` remain restricted to the authenticated user's own rows.

Configure `AdminBootstrap:UserName` and `AdminBootstrap:Password` with User Secrets before starting the API. On startup, the API creates or promotes that account to the Admin role. Sign in at `/admin/login`; the dashboard at `/admin/apps` supports adding, editing and archiving app listings. Ratings are optional and left blank in starter entries; enter them only when you have a source. Archived apps remain in the database and disappear from public search.

## App cards and comparison

Listings are stored in SQL Server's `Apps` table and maintained through the admin dashboard. Public visitors can search by text/category, view details and select multiple apps for a side-by-side comparison. The comparison shows the listing data and optional rating; it does not invent a universal winner. The sample rows are starter listings; verify descriptions, platform support, pricing and external URLs before publishing them. Admins can set logos to local `/images/...` paths or HTTPS URLs.

## 4. Schema changes

The project currently uses `EnsureCreated` plus an idempotent additive startup SQL update for existing databases. For ongoing schema evolution, move to versioned EF Core migrations instead:

```powershell
dotnet add DL_ALL_IN_ONE package Microsoft.EntityFrameworkCore.Design --version 8.0.28
dotnet tool install --global dotnet-ef --version 8.0.28
dotnet ef migrations add InitialSchema --project DL_ALL_IN_ONE --startup-project ALL_IN_ONE_API --output-dir Migrations
dotnet ef database update --project DL_ALL_IN_ONE --startup-project ALL_IN_ONE_API
```

Once migrations are adopted, replace `EnsureCreatedAsync` in API startup with `MigrateAsync` and commit the generated migration files.

## Project responsibilities

- `ALLINONE_Client`: public discovery home, live search/filtering, app details, multi-app comparison, admin login and listing dashboard.
- `ALL_IN_ONE_API`: public app endpoints, admin-only CRUD endpoints, JWT validation, CORS and database bootstrap.
- `BL_ALL_IN_ONE`: password hashing, registration/login rules, app listing validation and catalog operations.
- `DL_ALL_IN_ONE`: SQL Server EF Core context, user/app/notification entities and repositories.
- `UL_ALL_IN_ONE`: shared request/response contracts and signed JWT issuer.
