# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Purpose

Exploration of integrating `Microsoft.AspNetCore.OpenApi` (the ASP.NET Core 10+ approach to OpenAPI document generation) with Azure Functions v4 running on the .NET isolated worker model.

## Commands

```bash
# Build
dotnet build src/func.csproj

# Run locally (requires Azure Functions Core Tools and Azurite for local storage)
cd src && func start

# Publish (CI does this via GitHub Actions)
dotnet publish src/func.csproj -c Release -o ./publish
```

Local development requires:
- [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local) v4
- [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) — `local.settings.json` points to `UseDevelopmentStorage=true`

Once running, the OpenAPI document is at: `http://localhost:7071/api/openapi/v1.json`

## Architecture

### Function App (`src/`)

**`Program.cs`** — Entry point. `builder.MapOpenApi()` is the single call that wires everything up (calls `AddOpenApi()` internally and registers the HTTP-trigger path transformer).

**`Hello.cs`** — Sample HTTP-triggered function. Uses `IActionResult` return type and `HttpRequest` parameter, enabled by `Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore`. Demonstrates `[EndpointSummary]` for OpenAPI metadata.

**`OpenApiExtensions.cs`** — Contains two things:
1. `MapOpenApi()` extension on `IHostApplicationBuilder` — calls `AddOpenApi()` and registers an `IOpenApiDocumentTransformer` delegate that scans the entry assembly for `[Function]` + `[HttpTrigger]` methods and injects them as OpenAPI paths.
2. `OpenApiDocumentAccessor` (internal) — resolves `IDocumentProvider` at runtime via reflection (no compile-time reference) and calls `GenerateAsync` to produce the JSON string served by `OpenApiFunction`.

**`OpenApiFunction.cs`** — HTTP-triggered function at `Route = "openapi/{documentName}.json"` that calls `OpenApiDocumentAccessor.GenerateAsync` and returns the JSON.

### Why `app.MapOpenApi()` cannot be used

The Functions host on port 7071 only proxies requests to the .NET worker for routes registered via `[HttpTrigger]` attributes. ASP.NET Core middleware, endpoint routing, and `IStartupFilter` are all bypassed for any other path — so the standard `app.MapOpenApi()` call never receives requests. The OpenAPI endpoint must be an actual Azure Function.

### Why `Microsoft.AspNetCore.OpenApi` produces empty `paths` without the transformer

`Microsoft.AspNetCore.OpenApi` discovers endpoints from ASP.NET Core's routing data source. Azure Functions HTTP triggers do not register themselves there — they route through the Functions middleware instead. The `AddHttpTriggerPaths` transformer in `OpenApiExtensions.cs` bridges this gap by scanning for `[HttpTrigger]` via reflection.

### Key API notes for `Microsoft.OpenApi` v2 (2.0.0)

- Documentation at <https://learn.microsoft.com/en-us/dotnet/api/microsoft.openapi>
- All model types moved from `Microsoft.OpenApi.Models` → `Microsoft.OpenApi` (root namespace). Using `Microsoft.OpenApi.Models` or `Microsoft.OpenApi.Writers` is a compile error.
- `OperationType` enum is gone — `OpenApiPathItem.AddOperation` now takes `System.Net.Http.HttpMethod`.
- `OpenApiResponses` is keyed by string status code with `IOpenApiResponse` values.

### `IDocumentProvider` reflection approach

`OpenApiDocumentProvider` (which implements `IDocumentProvider`) is `internal` in `Microsoft.AspNetCore.OpenApi.dll` under namespace `Microsoft.Extensions.ApiDescriptions` (note the plural — `ApiDescriptions`, not `ApiDescription`). `OpenApiDocumentAccessor` finds it by:

1. Getting the `OpenApiDocumentProvider` concrete type by name from the known assembly
2. Walking `.GetInterfaces()` to get the `IDocumentProvider` type object
3. Calling `services.GetService(interfaceType)` then `GenerateAsync` via reflection

### Infrastructure (`infra/`)

Single `main.bicep` file deploying to Azure Australia East:

- **Flex Consumption plan** (`FC1`) — serverless, scales to zero
- **Function App** with user-assigned managed identity (no storage key access)
- **Storage Account** (`stfuncopenapiaue`) with managed identity role assignments for Blob, Queue, Table Data Contributor + Blob Data Owner
- **Application Insights** + **Log Analytics** workspace
- Storage connection uses `AzureWebJobsStorage__credential: managedidentity` — no connection strings

### CI/CD (`.github/workflows/`)

- **`deploy-stack.yml`**: Triggered on push to `main` (paths: `infra/**`). Three parallel jobs — `deploy` (Bicep via Azure Deployment Stack), `build` (`dotnet publish`), `publish` (`azure/functions-action@v1` with `sku: flexconsumption` to `func-openapi-australiaeast`).
- **`deploy-what-if.yml`**: PR validation with Bicep what-if.
- **`destroy.yml`**: Tears down the deployment stack.

Authentication uses OIDC federated credentials. Required GitHub secrets: `AZURE_TENANT_ID`, `AZURE_CLIENT_ID`, `AZURE_SUBSCRIPTION_ID`, `RESOURCE_GROUP_NAME`.
