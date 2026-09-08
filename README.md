# Microblogging Application

MicrobloggingApp is a .NET microblogging platform with short posts, image uploads, timeline filtering, post management, and real-time updates through SignalR.

## Stack

- ASP.NET Core Web API (.NET 8)
- Razor Pages frontend (.NET 8)
- SQL Server
- Entity Framework Core
- Azure Blob Storage
- SignalR
- Hangfire
- ImageSharp

## Projects

- `MicrobloggingApp.API` - REST API, authentication, SignalR hub, background processing
- `MicrobloggingApp.Core` - application services and contracts
- `MicrobloggingApp.Data` - EF Core context, entities, and repositories
- `MicrobloggingApp.Frontend` - Razor Pages UI
- `MicrobloggingApp.Tests` - automated tests

## Configuration

The API expects these settings to be supplied through configuration or environment variables:

- `ConnectionStrings:DefaultConnection`
- `JwtSettings:SecretKey`
- `JwtSettings:Issuer`
- `JwtSettings:Audience`
- `AzureBlobStorage:ConnectionString`
- `AzureBlobStorage:ContainerName`
- `Cors:AllowedOrigins`

The JWT secret must be at least 32 characters long.

The frontend expects:

- `ApiBaseUrl`
- `SignalRHubUrl`

For Azure App Service, configure these values under Application settings. Nested ASP.NET Core configuration keys can be supplied with `__`, for example `JwtSettings__SecretKey`.

## Local Development

Restore and build the solution:

```bash
dotnet restore MicrobloggingApp.sln
dotnet build MicrobloggingApp.sln --configuration Release
```

Run the API:

```bash
dotnet run --project MicrobloggingApp.API
```

Run the frontend:

```bash
dotnet run --project MicrobloggingApp.Frontend
```

The API exposes `/health` for a basic availability check.

## Manual Deployment

### API

```bash
dotnet publish MicrobloggingApp.API/MicrobloggingApp.API.csproj --configuration Release --output ./publish/api
```

Deploy the contents of `./publish/api` to the API hosting service and configure the required application settings.

### Frontend

```bash
dotnet publish MicrobloggingApp.Frontend/MicrobloggingApp.Frontend.csproj --configuration Release --output ./publish/frontend
```

Deploy the contents of `./publish/frontend` to the frontend hosting service and set `ApiBaseUrl` and `SignalRHubUrl` to the deployed API endpoints.

No automated deployment workflow is required. Deployment is intentionally manual.
