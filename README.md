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


### Run

Open two terminals from the solution root.

Terminal 1:

```bash
dotnet run --project src/METS.StockReplenishment.Api --launch-profile http
```

Terminal 2:

```bash
dotnet run --project src/METS.StockReplenishment.Blazor --launch-profile http
```

Default URLs:

- API: `http://localhost:5078`

- Blazor UI: `http://localhost:5295`


Then open the Blazor app in your browser:

- `http://localhost:5295`


### Run Tests

From the solution root, run:

```bash
dotnet test
```


## Notes
- The API uses an in-memory database.
- Seed data is loaded automatically on startup in Development.
- HTTP is used by default to keep local setup simple.


## Project References

Startup and port configuration can be found here:

- src/METS.StockReplenishment.Api/Properties/launchSettings.json
- src/METS.StockReplenishment.Blazor/Properties/launchSettings.json
- src/METS.StockReplenishment.Api/Program.cs
- src/METS.StockReplenishment.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs
