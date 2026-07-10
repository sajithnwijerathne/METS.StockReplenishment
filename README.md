# METS.StockReplenishment

## Build and Run

This solution contains separate projects for the API and the Blazor front end.

### Prerequisites

- .NET 10 SDK installed

You can verify your SDK with:

```bash
dotnet --version
```

### Build the Solution

From the solution root, run:

```bash
dotnet build METS.StockReplenishment.sln
```

This builds all projects in the solution, including:

- API
- Application
- Infrastructure
- Blazor
- Tests

### Run the API

Start the API project in its own terminal:

```bash
dotnet run --project src/METS.StockReplenishment.Api --launch-profile http
```

Default API URLs:

- HTTP: `http://localhost:5078`
- HTTPS: `https://localhost:7029`

Notes:

- The API uses an in-memory database.
- Seed data is loaded automatically on startup in Development.

### Run the Blazor Front End

Start the Blazor project in a separate terminal:

```bash
dotnet run --project src/METS.StockReplenishment.Blazor --launch-profile http
```

Default Blazor URLs:

- HTTP: `http://localhost:5295`
- HTTPS: `https://localhost:7103`

### Run Both Projects

Open two terminals from the solution root.

Terminal 1:

```bash
dotnet run --project src/METS.StockReplenishment.Api --launch-profile http
```

Terminal 2:

```bash
dotnet run --project src/METS.StockReplenishment.Blazor --launch-profile http
```

Then open the Blazor app in your browser:

- `http://localhost:5295`

### Useful Commands

Build a single project:

```bash
dotnet build src/METS.StockReplenishment.Api/METS.StockReplenishment.Api.csproj
dotnet build src/METS.StockReplenishment.Blazor/METS.StockReplenishment.Blazor.csproj
```

Run with HTTPS instead of HTTP:

```bash
dotnet run --project src/METS.StockReplenishment.Api --launch-profile https
dotnet run --project src/METS.StockReplenishment.Blazor --launch-profile https
```

### Project References

Startup and port configuration can be found here:

- src/METS.StockReplenishment.Api/Properties/launchSettings.json
- src/METS.StockReplenishment.Blazor/Properties/launchSettings.json
- src/METS.StockReplenishment.Api/Program.cs
- src/METS.StockReplenishment.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs
