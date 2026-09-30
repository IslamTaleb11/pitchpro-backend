# PitchPro (Frontend)
github.com/islamTaleb11/pitchpro-frontend

# PitchPro API (Backend)

This repository contains the backend API for PitchPro (ASP.NET Core, .NET 8). This README documents how to install, configure, and run the project with the specifics you provided: ADO.NET for data access, Chargily for payments, Azure Blob Storage for images, and a database .bak file included in the repository.

## About

Backend API for PitchPro built on .NET 8. Data access is implemented using ADO.NET (no EF Core migrations). Images are stored in Azure Blob Storage. Chargily is used for payment processing.

Solution path: `PitchProAPI/PitchProAPI.sln`


## Features

### 🔐 Authentication & Authorization

* JWT-based authentication with login, access tokens, and refresh tokens.
* Refresh token lifecycle management and revocation.
* Role-based authorization (e.g., President).
* Email verification enforcement through `[EmailVerified]` middleware.
* Rate limiting with:

  * `AuthLimiter` — IP-based limiting.
  * `UserLimiter` — per-user and per-endpoint limiting.
* Custom model validation responses.

### 👤 User & Identity

* User registration and retrieval.
* Login and token refresh endpoints.
* JWT upgrade endpoint for synchronizing plan and subscription claims.

### ⚽ Club Management

* Create clubs with crest image upload using Azure Blob Storage.
* Paginated club listing.
* Get club by ID.
* Delete clubs.
* Club subscription resolution and plan management.

### 🧑‍🤝‍🧑 Players & Person Entities

* Create, update, and retrieve players.
* Player photo uploads using Azure Blob Storage.
* Query players by category.
* Query players for match call-ups.
* Player injury management.
* Person and staff management.

### 🏆 Matches

* Create and update matches.
* Set match results and mark matches as completed.
* Match call-up management.
* Match events and attendance management.
* Paginated queries for completed matches by category.

### 🏋️ Training Sessions

* Create and update training sessions.
* Mark training sessions as completed.
* Paginated list of completed sessions.
* Training attendance management.
* Retrieve players by category.

### 💳 Payments & Subscriptions

* Chargily payment integration.
* Create checkout sessions.
* Webhook handling and validation middleware.
* `PlanLimitService` for enforcing Free and Premium plan limits.
* Subscription expiry and automatic plan downgrade handling.

### 📊 Dashboard & Analytics

* Dashboard summary metrics.
* Club statistics and counts.

### 👨‍💼 Admin & Roles

* Role management endpoints.
* Staff management endpoints.

### 📋 Attendance, Call-ups & Events

* Match attendance management.
* Training attendance management.
* Match call-up player endpoints.
* Match event logging.

### 🖼️ Image & File Handling

* Azure Blob Storage integration through `BlobService`.
* Image validation helpers.
* Request size limits.
* Configurable blob containers through application settings.

### 📧 Email & Notifications

* Email service integration through `IEmailService`.
* Brevo configuration support.
* Email verification flow and endpoints.

### ⚙️ Infrastructure & Middleware

* CORS configuration for frontend origins.
* Swagger / OpenAPI documentation.
* Rate limiting using `System.Threading.RateLimiting`.
* Dynamic port binding for cloud hosting environments.
* HTTP request helper utilities.
* Client IP and device information extraction.

### 🗄️ Data Access & Persistence

* ADO.NET-based `DataAccessLayer` and `BusinessLayer`.
* No Entity Framework Core.
* SQL Server database.
* `PitchPro.bak` included for database restoration.
* DTOs and services for domain operations.

### 🛠️ Developer & Runtime Aids

* `launchSettings.json`.
* Dockerfile.
* HTTP request collection.
* `appsettings.json` and `appsettings.Development.json` placeholders.

### 🔧 Helpers & Services

* `GeneralHelper` utilities.
* Image validation.
* JWT generation.
* `RefreshTokenService`.
* `PlanLimitService`.
* `EmailService`.
* `IMemoryCache` support.
* `HttpClient` registration.



## Prerequisites

- Windows or Linux/macOS with .NET 8 SDK installed: https://dotnet.microsoft.com/download/dotnet/8.0
- SQL Server instance (Developer / Express / Managed) for restoring the provided `.bak` file
- Azure Storage account + connection string
- Chargily account and API credentials
- Git
- (Optional) Visual Studio 2022/2026 or VS Code

## Project layout

- PitchProAPI/           - main solution and web API project
- database.bak           - SQL Server backup included in the repository root (restore to your SQL Server)
- PitchProAPI/appsettings.json - primary configuration file used by the app (override with env vars or user secrets)

Adjust paths if your copy differs.

## Configuration

The app reads configuration from `appsettings.json` and environment variables. Important settings you must provide:

- ConnectionStrings:DefaultConnection - ADO.NET connection string to your SQL Server database
- Chargily:ApiKey - Your Chargily API key (or whichever credentials Chargily requires)
- Azure:BlobStorageConnectionString - Azure Storage connection string
- Azure:ImagesContainer - Blob container name used for images

Example minimal appsettings.json (replace placeholders):

```
{
  "ConnectionStrings": {
	"DefaultConnection": "Server=localhost;Database=PitchProDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;"
  },
  "Chargily": {
	"ApiKey": "YOUR_CHARGILY_API_KEY"
  },
  "Azure": {
	"BlobStorageConnectionString": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net",
	"ImagesContainer": "pitchpro-images"
  },
  "Logging": {
	"LogLevel": { "Default": "Information" }
  }
}
```

Security notes:
- Do NOT commit production secrets to source control. Use environment variables or `dotnet user-secrets` for development.

Set environment variables in PowerShell before running (example):

```
$env:ConnectionStrings__DefaultConnection = "Server=...;Database=PitchProDb;User Id=...;Password=...;"
$env:Chargily__ApiKey = "..."
$env:Azure__BlobStorageConnectionString = "..."
$env:Azure__ImagesContainer = "pitchpro-images"
```

## Restore the database from database.bak

There is a `PitchPro.bak` file in the repository root. You must restore it to your SQL Server instance before running the API (unless you will create your own database and schema).

Recommended ways to restore:

1) Using SQL Server Management Studio (SSMS)
   - Copy `PitchPro.bak` to the SQL Server backup folder, or make it accessible from the server.
   - In SSMS: Right-click Databases -> Restore Database -> Device -> select the .bak file -> provide new database name -> Files -> update the physical paths (MDF/LDF) if needed -> OK.

2) Using sqlcmd / T-SQL (example). Edit paths and database name as needed:

```sql
RESTORE DATABASE [PitchProDb]
FROM DISK = N'C:\path\to\your\repo\database.bak'
WITH MOVE 'PitchPro_Data' TO 'C:\Program Files\Microsoft SQL Server\MSSQL15.MSSQLSERVER\MSSQL\DATA\PitchProDb.mdf',
	 MOVE 'PitchPro_Log'  TO 'C:\Program Files\Microsoft SQL Server\MSSQL15.MSSQLSERVER\MSSQL\DATA\PitchProDb_log.ldf',
	 REPLACE;
```

Then run in PowerShell (adjust -S and -U/-P or use integrated auth):

```
sqlcmd -S localhost -U sa -P "YourStrong!Passw0rd" -i restore.sql
```

If you run into file path errors, copy the .bak to a folder the SQL Server service account can access.

Note: The logical file names (`PitchPro_Data`, `PitchPro_Log`) in the RESTORE statement must match those inside the .bak. If you are unsure, run:

```
RESTORE FILELISTONLY FROM DISK = N'C:\path\to\database.bak'
```

## Running locally

1. Open a terminal at the repo root (PowerShell preferred):

```
git clone <repo>  # if not already cloned
cd C:\Users\MSI\Desktop\PitchPro\pitchpro-backend
dotnet restore
dotnet build PitchProAPI/PitchProAPI.sln -c Debug
```

2. Ensure database restored and connection string configured (see Configuration & Restore DB).

3. Run the API:

```
dotnet run --project PitchProAPI/PitchProAPI.csproj -c Debug
```

4. Open the configured URL (console output or launchSettings.json), typically https://localhost:5001 or http://localhost:5000.

Because the project uses ADO.NET, there are no EF migrations to run.

## Chargily integration

- The project expects Chargily configuration (API key, callback URLs) in configuration under `Chargily` section. Update `appsettings.json` or environment variables.
- Implement or verify payment endpoints in the API that call Chargily HTTP endpoints using HttpClient. Keep the Chargily API key secret.
- Provide webhook/callback endpoint and configure it in the Chargily dashboard so payment state updates can be received.

Example placeholders in code (pseudo):

```
var http = new HttpClient();
http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", configuration["Chargily:ApiKey"]);
// POST payment creation ...
```

## Azure Blob Storage for images

- Configure `Azure:BlobStorageConnectionString` and `Azure:ImagesContainer` in configuration.
- The API should use Azure.Storage.Blobs (NuGet) or SDK to upload/download images.

Example upload pseudocode (C#):

```csharp
var blobService = new BlobServiceClient(blobConnectionString);
var container = blobService.GetBlobContainerClient(containerName);
await container.CreateIfNotExistsAsync();
var blob = container.GetBlobClient(fileName);
await blob.UploadAsync(stream, overwrite: true);
var url = blob.Uri; // store this URL in DB
```

Make sure the container's access policy suits your use case (private vs public). For private containers, serve images through the API or generate SAS tokens.

## Notes and troubleshooting

- If the API fails to connect to the database, verify the connection string and that SQL Server allows TCP connections.
- If restoring .bak fails with permission errors, copy .bak into the SQL Server machine's local folder or run restore from an account with access.
- For local development secrets, use `dotnet user-secrets` or environment variables instead of committing secrets.
- If you need me to add a properly filled appsettings.Development.json, or to create code examples in the project (Chargily client, Azure Blob helper), tell me which files/classes to modify and I will implement them.

## Contributing

- Fork, create a branch, implement changes, add tests if applicable, and open a PR.

---

If you want, I can now create/update the actual README.md file in the repository (I just added it). I can also modify appsettings.json with example keys (non-secret placeholders) or add helper classes for Azure/Chargily integration. Tell me which next step you want.
