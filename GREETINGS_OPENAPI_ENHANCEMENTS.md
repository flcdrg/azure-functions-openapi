# Greeting Function OpenAPI Metadata Enhancements

## Overview

The Greeting Function has been significantly enhanced to demonstrate comprehensive usage of OpenAPI metadata attributes as defined in the [Microsoft OpenAPI Metadata Documentation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/include-metadata?view=aspnetcore-10.0).

## Metadata Attributes Used

### Endpoint-Level Metadata

| Attribute | Methods | Purpose |
|-----------|---------|---------|
| `[EndpointSummary]` | All methods | Short, one-line summary of the endpoint |
| `[EndpointDescription]` | All methods | Detailed description supporting markdown formatting |
| `[EndpointName]` | All methods | Defines the operationId in OpenAPI document |
| `[Tags]` | All methods | Categorizes endpoints by tags (e.g., "greetings", "retrieval", "creation") |
| `[Obsolete]` | Internal() | Marks endpoint as deprecated |
| `[ApiExplorerSettings(IgnoreApi = true)]` | Internal() | Excludes endpoint from OpenAPI documentation |

### Request/Response Metadata

| Attribute | Methods | Purpose |
|-----------|---------|---------|
| `[Consumes]` | Post(), Put() | Specifies accepted media types (application/json) |
| `[ProducesResponseType<T>]` | Get(), Post(), Put(), Delete() | Defines response types with status codes and descriptions |
| `[ProducesResponseType]` | Delete() | Response without response body (204 No Content) |

### Parameter Metadata

| Attribute | Usage | Purpose |
|-----------|-------|---------|
| `[Description]` | Route & body parameters | Provides parameter documentation in OpenAPI |
| `[Required]` | Request model properties | Marks fields as required in request body |
| `[MinLength]` / `[MaxLength]` | Request model properties | Defines string length constraints |
| `[RegularExpression]` | Response model properties | Validates format (e.g., BCP-47 language codes) |
| `[DefaultValue]` | Request model properties | Sets default values in examples |

### Model Documentation

All request/response models include:
- XML doc comments (`/// <summary>`)
- `[Description]` attributes on properties
- Validation attributes (`[Required]`, `[MinLength]`, `[MaxLength]`, `[RegularExpression]`)
- `[DefaultValue]` for optional fields

## Enhanced Functionality

### Original Methods (Enhanced)

#### GET /api/greetings/{name}?lang=en
- **Summary**: Get a greeting
- **Tags**: greetings, retrieval
- **Response Types**: 
  - 200 OK (GreetingResponse)
  - 400 Bad Request (Invalid name)
  - 404 Not Found (Unsupported language)

#### POST /api/greetings
- **Summary**: Create a greeting
- **Tags**: greetings, creation
- **Request Body**: GreetingRequest (application/json)
- **Response Types**:
  - 200 OK (GreetingResponse)
  - 400 Bad Request (Validation failure)
  - 422 Unprocessable Entity (Malformed JSON)

### New Methods

#### PUT /api/greetings/{name}
- **Summary**: Update a greeting's language
- **Tags**: greetings, modification
- **Request Body**: GreetingRequest (application/json)
- **Parameter**: name (1-50 characters, with validation)
- **Response Types**:
  - 200 OK (GreetingResponse)
  - 400 Bad Request (Invalid parameters or request)
  - 404 Not Found (Unsupported language)
- **Demonstrates**: Request body binding with route parameters, comprehensive error handling

#### DELETE /api/greetings/{name}
- **Summary**: Delete a greeting
- **Tags**: greetings, deletion
- **Parameter**: name (1-50 characters, with validation)
- **Response Types**:
  - 204 No Content (Successful deletion)
  - 400 Bad Request (Invalid parameters)
  - 404 Not Found (Greeting not found)
- **Demonstrates**: 204 No Content responses for successful deletions

### Deprecated Method (Excluded from API)

#### [DEPRECATED] GET /api/greetings/internal (GreetingsInternal)
- **Status**: Deprecated via `[Obsolete]` attribute
- **Exclusion**: `[ApiExplorerSettings(IgnoreApi = true)]`
- **Purpose**: Demonstrates deprecation and exclusion patterns

## Data Models

### GreetingResponse
```csharp
public record GreetingResponse(
    [property: Description("The personalised greeting message.")]
    string Message,

    [property: Description("The BCP-47 language code used.")]
    [property: RegularExpression(@"^[a-z]{2}$")]
    string Lang
);
```

### GreetingRequest
```csharp
public record GreetingRequest(
    [property: Required]
    [property: MinLength(1), MaxLength(50)]
    [property: Description("The name to greet.")]
    string Name,

    [property: DefaultValue("en")]
    [property: Description("BCP-47 language code. Supported: en, es, fr.")]
    string? Lang
);
```

### LanguageNotSupportedError
```csharp
public record LanguageNotSupportedError(
    [property: Description("Error message describing the unsupported language.")]
    string Detail
);
```

## OpenAPI Transformer Enhancements

The `OpenApiExtensions.cs` has been updated to support additional metadata:

### Changes to AddHttpTriggerPaths Transformer

1. **Obsolete Attribute Support**: Detects `[Obsolete]` attribute on methods
   ```csharp
   var deprecatedAttr = method.GetCustomAttribute<ObsoleteAttribute>();
   ```

2. **Deprecated Flag in Operation**: Sets the `Deprecated` property when present
   ```csharp
   Deprecated = deprecatedAttr is not null,
   ```

3. **Enhanced Type Support**: Properly handles:
   - `IOpenApiParameter` interface for parameters
   - Multiple response types from `[ProducesResponseType]` attributes
   - Request body metadata from `[Consumes]` attributes
   - Endpoint routing and route parameters

## Attributes & Metadata Coverage Matrix

### ✅ Fully Implemented
- [EndpointSummary]
- [EndpointDescription]
- [EndpointName]
- [Tags]
- [Consumes]
- [ProducesResponseType<T>]
- [ProducesResponseType]
- [Description] (on parameters)
- [Required] (on model properties)
- [MinLength] / [MaxLength]
- [RegularExpression]
- [DefaultValue]
- [ApiExplorerSettings(IgnoreApi)]
- [Obsolete]

### ℹ️ Contextual Notes
- **Query Parameters**: Documented via HttpRequest.Query in method body with inline comments
- **Header Parameters**: Not demonstrated in current implementation (can be added via [FromHeader] pattern)
- **Form Parameters**: Not demonstrated in current implementation (can be added via [FromForm] pattern)

## Usage in OpenAPI Document

When the function app runs, the OpenAPI document includes:

1. **Path Items**: 
   - `/api/greetings/{name}` (GET, PUT, DELETE)
   - `/api/greetings` (POST)

2. **Operations**:
   - Summary and description for each
   - OperationId for unique reference
   - Multiple response types with status codes
   - Parameter definitions with validation

3. **Schemas**:
   - GreetingRequest and GreetingResponse types
   - Property descriptions and constraints
   - Validation rules

4. **Deprecation**:
   - Deprecated operations marked in document
   - Excluded operations not appearing in document

## Testing the Enhancements

```bash
# Get the OpenAPI document
curl http://localhost:7071/api/openapi/v1.json | jq .

# Test the enhanced endpoints
curl http://localhost:7071/api/greetings/World?lang=es
curl -X POST http://localhost:7071/api/greetings -H "Content-Type: application/json" -d '{"name":"Alice","lang":"fr"}'
curl -X PUT http://localhost:7071/api/greetings/Bob -H "Content-Type: application/json" -d '{"lang":"en"}'
curl -X DELETE http://localhost:7071/api/greetings/Charlie
```

## References

- [Microsoft ASP.NET Core OpenAPI Metadata Documentation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/include-metadata?view=aspnetcore-10.0)
- [OpenAPI 3.0.3 Specification](https://spec.openapis.org/oas/v3.0.3)
- [Microsoft.AspNetCore.OpenApi NuGet Package](https://www.nuget.org/packages/Microsoft.AspNetCore.OpenApi/)
- [Microsoft.OpenApi v2.0.0 NuGet Package](https://www.nuget.org/packages/Microsoft.OpenApi/2.0.0)
