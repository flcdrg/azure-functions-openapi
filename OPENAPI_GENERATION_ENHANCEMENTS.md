# OpenAPI Document Generation Enhancements

## Overview

The OpenAPI document generation has been significantly enhanced to capture more comprehensive metadata from Azure Functions attributes. This document describes the improvements made to the `OpenApiExtensions.cs` transformer that generates OpenAPI specifications from Azure Function HTTP triggers.

## Key Enhancements

### 1. **Parameter Documentation with Descriptions**

**What Changed:**
- Route parameters now include descriptions extracted from `[Description]` attributes

**Example:**
```json
{
  "name": "name",
  "in": "path",
  "required": true,
  "description": "The name of the greeting to delete (1-50 characters)."
}
```

**How it Works:**
- The `BuildParameters()` method scans method parameters for matching route segments
- Extracts `[Description]` attribute from the method parameter
- Applies the description to the OpenAPI parameter definition

### 2. **Tags Registration in Document**

**What Changed:**
- All tags used in endpoints are now registered in the document's `tags` collection
- Tags appear at the top level of the OpenAPI document

**Example:**
```json
"tags": [
  { "name": "greetings" },
  { "name": "retrieval" },
  { "name": "creation" },
  { "name": "modification" },
  { "name": "deletion" },
  { "name": "legacy" }
]
```

**How it Works:**
- As operations are processed, tags are collected in a local list
- Duplicate tags are avoided using `Any()` check
- Tags are assigned to `document.Tags` as a `HashSet<OpenApiTag>` at the end

### 3. **Comprehensive Response Metadata**

**What Changed:**
- Response types now include detailed status code information from `[ProducesResponseType]` attributes
- Responses include both success and error states with proper descriptions

**Example:**
```json
"responses": {
  "200": {
    "description": "Success"
  },
  "204": {
    "description": "The greeting was successfully deleted."
  },
  "400": {
    "description": "The name parameter is invalid."
  },
  "404": {
    "description": "The greeting does not exist."
  }
}
```

**How it Works:**
- The `ProducesResponseTypeAttribute` provides status codes and descriptions
- Each attribute generates a response entry
- Default descriptions are provided for common HTTP status codes

### 4. **Deprecated Operations**

**What Changed:**
- Operations marked with `[Obsolete]` attribute are flagged as deprecated in the OpenAPI document

**Example:**
```json
{
  "deprecated": true,
  "summary": "[DEPRECATED] Legacy greeting endpoint",
  "description": "This endpoint is deprecated and will be removed in a future version."
}
```

**How it Works:**
- The transformer detects `[Obsolete]` attribute on methods
- Sets the `Deprecated` property to `true` on the operation
- Works in conjunction with summary and description for clarity

### 5. **Request Body Specifications**

**What Changed:**
- Request bodies from `[Consumes]` attributes are properly documented
- Content types are specified with empty MediaType objects

**Example:**
```json
"requestBody": {
  "required": true,
  "content": {
    "application/json": {}
  }
}
```

**How it Works:**
- The `[Consumes]` attribute specifies accepted media types
- Creates OpenApiRequestBody with required flag
- Maps content types to OpenApiMediaType objects

## Metadata Attributes Collected

### Operation Level
| Attribute | Field | Purpose |
|-----------|-------|---------|
| `[EndpointSummary]` | `Summary` | Short one-line endpoint description |
| `[EndpointDescription]` | `Description` | Detailed operation description |
| `[EndpointName]` | `OperationId` | Unique operation identifier |
| `[Obsolete]` | `Deprecated` | Marks operation as deprecated |
| `[ApiExplorerSettings(IgnoreApi = true)]` | (excluded) | Removes operation from document |

### Response Level
| Attribute | Usage | Purpose |
|-----------|-------|---------|
| `[ProducesResponseType]` | Status codes & descriptions | Response outcomes with codes |
| `StatusCode` | Response key | HTTP status code (200, 400, etc.) |
| `Description` | Response description | Explanation of response meaning |

### Parameter Level
| Attribute | Usage | Purpose |
|-----------|-------|---------|
| `[Description]` | Parameter description | Documents route parameters |
| Route binding | Path in/required | Auto-detected route parameters |

### Request/Response Body
| Attribute | Usage | Purpose |
|-----------|-------|---------|
| `[Consumes]` | Content types | Request media types (e.g., application/json) |

### Tag Organization
| Attribute | Usage | Purpose |
|-----------|-------|---------|
| `[Tags]` | Operation tags | Groups related operations |

## Data Flow

```
Azure Function Method
    ↓
Scan for [Function] attribute
    ↓
Extract metadata attributes:
  - [EndpointSummary]
  - [EndpointDescription]
  - [EndpointName]
  - [Tags]
  - [Consumes]
  - [ProducesResponseType]
  - [Obsolete]
  - [ApiExplorerSettings(IgnoreApi)]
    ↓
Build Operation object:
  - Summary
  - Description
  - OperationId
  - Responses (from ProducesResponseType)
  - Parameters (with descriptions)
  - RequestBody (from Consumes)
  - Deprecated (from Obsolete)
    ↓
Register tags in document.Tags
    ↓
Add operation to path/method in document.Paths
    ↓
Generate OpenAPI 3.1.1 Document
```

## Implementation Details

### BuildParameters() Method
```csharp
private static List<IOpenApiParameter> BuildParameters(MethodInfo method, List<string> routeParams)
```
- Takes method info and route parameter names
- Matches route parameters with method parameters
- Extracts descriptions from `[Description]` attributes
- Returns list of `IOpenApiParameter` objects

### BuildResponses() Method
```csharp
private static OpenApiResponses BuildResponses(
    List<ProducesResponseTypeAttribute> attrs,
    Assembly assembly)
```
- Processes all `[ProducesResponseType]` attributes
- Creates response entry for each status code
- Provides default descriptions for standard status codes
- Returns `OpenApiResponses` collection

### Tag Collection
- Local `List<OpenApiTag>` collects tags during traversal
- Duplicate checking prevents registry conflicts
- Assigned to `document.Tags` as `HashSet<OpenApiTag>` at completion

## Example Generated Document Structure

```json
{
  "openapi": "3.1.1",
  "info": {
    "title": "func | v1",
    "version": "1.0.0"
  },
  "tags": [
    { "name": "greetings" },
    { "name": "retrieval" }
  ],
  "paths": {
    "/greetings/{name}": {
      "get": {
        "summary": "Get a greeting",
        "description": "Returns a personalised greeting...",
        "operationId": "GetGreeting",
        "deprecated": false,
        "parameters": [
          {
            "name": "name",
            "in": "path",
            "required": true,
            "description": "The name to include in the greeting (1-50 characters)."
          }
        ],
        "responses": {
          "200": {
            "description": "Success"
          },
          "400": {
            "description": "The name parameter is invalid or missing."
          },
          "404": {
            "description": "The requested language is not supported."
          }
        }
      }
    },
    "/greetings": {
      "post": {
        "summary": "Create a greeting",
        "description": "Returns a greeting for the name and language provided in the request body.",
        "operationId": "CreateGreeting",
        "requestBody": {
          "required": true,
          "content": {
            "application/json": {}
          }
        },
        "responses": {
          "200": {
            "description": "Success"
          },
          "400": {
            "description": "The request body failed validation or unsupported language."
          },
          "422": {
            "description": "The request body is malformed or missing required fields."
          }
        }
      }
    }
  }
}
```

## Comparison: Before vs After

### Parameter Documentation

**Before:**
```json
{
  "name": "name",
  "in": "path",
  "required": true
}
```

**After:**
```json
{
  "name": "name",
  "in": "path",
  "required": true,
  "description": "The name of the person whose greeting to update."
}
```

### Response Documentation

**Before:**
```json
"responses": {
  "200": {
    "description": "Success"
  }
}
```

**After:**
```json
"responses": {
  "200": {
    "description": "Success"
  },
  "204": {
    "description": "The greeting was successfully deleted."
  },
  "400": {
    "description": "The name parameter is invalid."
  },
  "404": {
    "description": "The greeting does not exist."
  }
}
```

### Top-Level Tags

**Before:**
- No tags in document

**After:**
```json
"tags": [
  { "name": "greetings" },
  { "name": "retrieval" },
  { "name": "creation" },
  { "name": "modification" },
  { "name": "deletion" },
  { "name": "legacy" }
]
```

## Attributes Now Supported

✅ `[EndpointSummary]` - Operation summary
✅ `[EndpointDescription]` - Operation description
✅ `[EndpointName]` - Operation ID
✅ `[Tags]` - Operation tags & document tags
✅ `[Consumes]` - Request body content types
✅ `[ProducesResponseType]` - Response status codes and descriptions
✅ `[Description]` - Parameter descriptions
✅ `[Obsolete]` - Deprecation marking
✅ `[ApiExplorerSettings(IgnoreApi = true)]` - Exclusion from API
✅ Route parameter detection - Automatic from route templates

## Future Enhancement Opportunities

1. **Schema Generation** - Generate full JSON Schema for request/response bodies
2. **Query Parameters** - Document query string parameters with validation rules
3. **Header Parameters** - Support for `[FromHeader]` attributed parameters
4. **Default Values** - Include `[DefaultValue]` in parameter documentation
5. **Validation Constraints** - Surface `[MinLength]`, `[MaxLength]`, `[RegularExpression]` in schemas
6. **Examples** - Include example values and responses
7. **Security Schemes** - Support for API key, OAuth2, etc.

## Files Modified

- `src/OpenApiExtensions.cs` - Enhanced `AddHttpTriggerPaths()` and supporting methods
- Added `using System.ComponentModel;` - For `[Description]` attribute

## Build Status

✅ Build succeeded with no warnings or errors
