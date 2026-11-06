# AgriStream 
# AgriStream IoT: Real-Time Processing System

CLDV7112 Cloud Development Practicum, built with ASP.NET Core MVC and Azure.

**Student:** [Your name] (ST10346094)
**Live app:** https://st10346094cldvpracticum-hbbxevbefbbygme6.southafricanorth-01.azurewebsites.net
**Function App:** https://cloudpracticumfunctionsapp-avgdgbbqeue7ejh5.southafricanorth-01.azurewebsites.net

## Overview

AgriStream IoT collects soil sensor telemetry and drone imagery from commercial farms. This solution is a management web app plus a serverless pipeline that absorbs bursts of data without losing it.

## Solution structure

| Project | Description |
|---|---|
| `AgriStream` | ASP.NET Core MVC web app (.NET 10) hosted on Azure App Service |
| `AgriStream.Functions` | Azure Functions app (.NET 8 isolated worker), four functions |
| `AgriStream.LoadTest` | C# console app that simulates concurrent users for load testing |

## Azure services used

- **Azure App Service**: hosts the MVC app
- **Azure SQL Database**: users, roles and the device registry (Entity Framework Core code-first)
- **Azure Blob Storage**: drone images (`drone-images` container)
- **Azure Table Storage**: `Telemetry` and `ProcessingAudit` tables
- **Azure Storage Queue**: `irrigation-commands`
- **Azure Functions**: serverless processing pipeline
- **Azure Monitor**: metrics during load testing

## How it works

1. A user logs in (cookie authentication, roles: Farmer, Agronomist, Admin).
2. Drone images are uploaded to Blob Storage, which triggers Function 1.
3. The dashboard calls Function 2 to read the latest telemetry from Table Storage.
4. "Trigger Irrigation" calls Function 3, which places a command on the queue.
5. Function 4 reads the queue and updates the device status in Azure SQL.

## The four Azure Functions

| # | Function | Trigger | What it does |
|---|---|---|---|
| 1 | `DroneImageBlobTrigger` | Blob (`drone-images/{name}`) | Logs the upload and writes an audit entry to the `ProcessingAudit` table |
| 2 | `GetLatestTelemetry` | HTTP GET `/api/telemetry` | Returns the latest telemetry readings from Table Storage as JSON |
| 3 | `TriggerIrrigation` | HTTP POST `/api/irrigation` | Validates the command and puts it on the storage queue (output binding) |
| 4 | `ProcessIrrigationCommand` | Queue (`irrigation-commands`) | Updates the device status in Azure SQL |

## Design decisions

- Passwords are stored as salted hashes (`PasswordHasher`), never in plain text.
- Telemetry `PartitionKey` is the `DeviceId`; `RowKey` is reversed ticks so the newest reading sorts first.
- The web app does not wait for the database on irrigation commands: it queues them, so bursts do not slow the site down.
- Web and function tiers are stateless (cookie auth, state kept in SQL/Storage) so they can scale out.
- Functions are protected by function keys, and inputs are validated.

## Running locally

**Requirements:** Visual Studio 2022+, .NET 10 SDK and .NET 8 SDK, Azure Functions Core Tools, an Azure SQL database and an Azure Storage account.

1. Clone the repo.
2. In `AgriStream/appsettings.json`, replace the placeholders with your own values:
   - `ConnectionStrings:DefaultConnection`: Azure SQL connection string
   - `AzureStorage:ConnectionString`: storage account connection string
   - `FunctionApi:BaseUrl`: `http://localhost:7002/` (check the port printed by the Functions console)
3. In `AgriStream.Functions`, create `local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "YOUR_STORAGE_CONNECTION_STRING",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "SqlConnectionString": "YOUR_SQL_CONNECTION_STRING"
  }
}
```

4. Create the database: in Package Manager Console run `Update-Database` (default project `AgriStream`).
5. Set both `AgriStream` and `AgriStream.Functions` as startup projects and run.
6. A default Admin account is seeded on first run. See the Program.cs seed block for its username, and change the password before any real use.

## Deployed configuration

Secrets are not stored in this repository. In Azure they are set as App Service environment variables:

| Resource | Setting |
|---|---|
| Web App | `ConnectionStrings__DefaultConnection`, `AzureStorage__ConnectionString`, `AzureStorage__BlobContainer`, `FunctionApi__BaseUrl`, `FunctionApi__Key` |
| Function App | `AzureWebJobsStorage`, `SqlConnectionString` |

## Load testing

`AgriStream.LoadTest` simulates concurrent users against the live site and reports requests, failures, average response time and requests per second. Set `baseUrl` in `Program.cs` before running.

**Result summary:** about 90 requests per second with 0 failures and roughly 1.1 s average response for the first 70 seconds at 100 users, until the App Service plan's daily quota was reached (see the project document for details).

## Known limitations

- The App Service plan on the school subscription could not be scaled to a tier that supports autoscale, and the `microsoft.insights` provider was not registered, so Application Insights could not be created. Azure Monitor metrics were used instead. Planned autoscale rules (scale out at CPU > 30%, in at CPU < 15%, 1 to 3 instances) are documented in the project document.
- Sensor data is simulated through the Telemetry Simulator page.
