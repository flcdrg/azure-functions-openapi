# OpenAPI Attributes Quick Reference Guide

## How to Use OpenAPI Attributes in Your Azure Functions

This guide shows how the Greeting Function implements OpenAPI metadata attributes and how to apply them to your own Azure Functions.

---

## Quick Start: Minimal Example

```csharp
[EndpointSummary("Get user by ID")]
[EndpointDescription("Retrieves a user record by their unique identifier.")]
[Tags(["users", "retrieval"])]
[EndpointName("GetUserById")]
[ProducesResponseType<User>(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound, Description = "User not found.")]
[Function("GetUserById")]
public static IActionResult GetUserById(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/{id}")]
    HttpRequest req,
    [Description("The unique user identifier.")]
    string id)
{
    // Implementation
}
```

---

## All Attributes Explained

### 1. [EndpointSummary] — Short Description
**Purpose:** Brief one-line description of what the endpoint does

```csharp
[EndpointSummary("Get a greeting")]
public static IActionResult Get(...) { }
```

**Output:**
```json
{ "summary": "Get a greeting" }
```

---

### 2. [EndpointDescription] — Detailed Description
**Purpose:** Comprehensive description with additional context

```csharp
[EndpointDescription("Returns a personalised greeting for *name* in the requested language.")]
public static IActionResult Get(...) { }
```

**Output:**
```json
{ "description": "Returns a personalised greeting for *name* in the requested language." }
```

---

### 3. [EndpointName] — Operation ID
**Purpose:** Sets the unique operation identifier in OpenAPI

```csharp
[EndpointName("GetGreeting")]
public static IActionResult Get(...) { }
```

**Output:**
```json
{ "operationId": "GetGreeting" }
```

---

### 4. [Tags] — Categorization
**Purpose:** Organizes related endpoints under named groups

```csharp
[Tags(["greetings", "retrieval"])]
public static IActionResult Get(...) { }
```

**Output:**
```json
{
  "tags": [
    { "$ref": "#/components/tags/greetings" },
    { "$ref": "#/components/tags/retrieval" }
  ]
}
```

**Document Level:**
```json
{
  "tags": [
    { "name": "greetings" },
    { "name": "retrieval" },
    { "name": "creation" },
    { "name": "modification" },
    { "name": "deletion" }
  ]
}
```

---

### 5. [Consumes] — Request Media Type
**Purpose:** Documents what media types the endpoint accepts

```csharp
[Consumes("application/json")]
public static IActionResult Create(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "greetings")]
    HttpRequest req,
    [FromBody] CreateRequest body)
{ }
```

**Output:**
```json
{
  "requestBody": {
    "required": true,
    "content": {
      "application/json": {}
    }
  }
}
```

---

### 6. [ProducesResponseType] — Response Codes & Types
**Purpose:** Documents all possible response codes and descriptions

```csharp
[ProducesResponseType<GreetingResponse>(StatusCodes.Status200OK)]
[ProducesResponseType<ProblemDetails>(
    StatusCodes.Status404NotFound,
    mediaType: "application/problem+json",
    Description = "The greeting does not exist.")]
[ProducesResponseType<ProblemDetails>(
    StatusCodes.Status400BadRequest,
    mediaType: "application/problem+json",
    Description = "The name parameter is invalid.")]
public static IActionResult Get(...) { }
```

**Output:**
```json
{
  "responses": {
    "200": {
      "description": "Success"
    },
    "400": {
      "description": "The name parameter is invalid."
    },
    "404": {
      "description": "The greeting does not exist."
    }
  }
}
```

---

### 7. [Description] — Parameter Documentation
**Purpose:** Describes route parameters and their constraints

```csharp
public static IActionResult Get(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "greetings/{name}")]
    HttpRequest req,
    [Description("The name to include in the greeting (1-50 characters).")]
    string name)
{ }
```

**Output:**
```json
{
  "parameters": [
    {
      "name": "name",
      "in": "path",
      "description": "The name to include in the greeting (1-50 characters).",
      "required": true
    }
  ]
}
```

---

### 8. [Obsolete] — Deprecation
**Purpose:** Marks endpoints as deprecated

```csharp
[Obsolete("Use GetGreeting endpoint instead")]
[EndpointSummary("[DEPRECATED] Legacy greeting endpoint")]
public static IActionResult Internal(...) { }
```

**Output:**
```json
{
  "deprecated": true,
  "summary": "[DEPRECATED] Legacy greeting endpoint"
}
```

---

### 9. [ApiExplorerSettings(IgnoreApi = true)] — Hide from Documentation
**Purpose:** Completely excludes endpoint from OpenAPI document

```csharp
[ApiExplorerSettings(IgnoreApi = true)]
public static IActionResult InternalOnly(...) { }
```

**Result:** Not included in OpenAPI document

---

## Complete Example: Full CRUD Function

```csharp
using System.ComponentModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore;

namespace MyApp;

[EndpointSummary("Get a user")]
[EndpointDescription("Retrieves a specific user by their unique identifier.")]
[Tags(["users", "retrieval"])]
[EndpointName("GetUser")]
[ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
[ProducesResponseType<ProblemDetails>(
    StatusCodes.Status404NotFound,
    mediaType: "application/problem+json",
    Description = "User not found.")]
[ProducesResponseType<ProblemDetails>(
    StatusCodes.Status400BadRequest,
    mediaType: "application/problem+json",
    Description = "Invalid user ID format.")]
[Function("GetUser")]
public static IActionResult GetUser(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/{id}")]
    HttpRequest req,
    [Description("The unique user identifier (UUID format).")]
    string id)
{
    if (string.IsNullOrEmpty(id))
        return new BadRequestResult();
    
    // Get user by id
    return new OkObjectResult(new UserResponse { Id = id, Name = "John" });
}

[EndpointSummary("Create a user")]
[EndpointDescription("Creates a new user record with the provided information.")]
[Tags(["users", "creation"])]
[EndpointName("CreateUser")]
[Consumes("application/json")]
[ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
[ProducesResponseType<ProblemDetails>(
    StatusCodes.Status400BadRequest,
    mediaType: "application/problem+json",
    Description = "Invalid request payload.")]
[ProducesResponseType<ProblemDetails>(
    StatusCodes.Status422UnprocessableEntity,
    mediaType: "application/problem+json",
    Description = "Request body is missing required fields.")]
[Function("CreateUser")]
public static IActionResult CreateUser(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "users")]
    HttpRequest req,
    [FromBody] CreateUserRequest request)
{
    if (string.IsNullOrEmpty(request?.Name))
        return new UnprocessableEntityResult();
    
    // Create user
    return new CreatedResult("/users/123", new UserResponse { Id = "123", Name = request.Name });
}

[EndpointSummary("Update a user")]
[EndpointDescription("Updates an existing user's information.")]
[Tags(["users", "modification"])]
[EndpointName("UpdateUser")]
[Consumes("application/json")]
[ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
[ProducesResponseType<ProblemDetails>(
    StatusCodes.Status404NotFound,
    mediaType: "application/problem+json",
    Description = "User not found.")]
[ProducesResponseType<ProblemDetails>(
    StatusCodes.Status400BadRequest,
    mediaType: "application/problem+json",
    Description = "Invalid user ID or request payload.")]
[Function("UpdateUser")]
public static IActionResult UpdateUser(
    [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "users/{id}")]
    HttpRequest req,
    [Description("The unique user identifier (UUID format).")]
    string id,
    [FromBody] UpdateUserRequest request)
{
    if (string.IsNullOrEmpty(id))
        return new BadRequestResult();
    
    // Update user
    return new OkObjectResult(new UserResponse { Id = id, Name = request.Name });
}

[EndpointSummary("Delete a user")]
[EndpointDescription("Permanently removes a user record from the system.")]
[Tags(["users", "deletion"])]
[EndpointName("DeleteUser")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType<ProblemDetails>(
    StatusCodes.Status404NotFound,
    mediaType: "application/problem+json",
    Description = "User not found.")]
[ProducesResponseType<ProblemDetails>(
    StatusCodes.Status400BadRequest,
    mediaType: "application/problem+json",
    Description = "Invalid user ID format.")]
[Function("DeleteUser")]
public static IActionResult DeleteUser(
    [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "users/{id}")]
    HttpRequest req,
    [Description("The unique user identifier (UUID format).")]
    string id)
{
    if (string.IsNullOrEmpty(id))
        return new BadRequestResult();
    
    // Delete user
    return new NoContentResult();
}

public class UserResponse
{
    public string Id { get; set; }
    public string Name { get; set; }
}

public class CreateUserRequest
{
    public string Name { get; set; }
}

public class UpdateUserRequest
{
    public string Name { get; set; }
}
```

---

## Best Practices

### 1. Always Provide Summary and Description
```csharp
✅ DO:
[EndpointSummary("Get user")]
[EndpointDescription("Retrieves user by ID")]

❌ DON'T:
[EndpointName("GetUser")]  // Only endpoint name
```

### 2. Use Multiple Tags for Better Organization
```csharp
✅ DO:
[Tags(["users", "retrieval"])]

❌ DON'T:
[Tags(["users"])]
```

### 3. Document All Response Codes
```csharp
✅ DO:
[ProducesResponseType<User>(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]

❌ DON'T:
[ProducesResponseType<User>(StatusCodes.Status200OK)]
```

### 4. Include Parameter Constraints in Descriptions
```csharp
✅ DO:
[Description("User ID (UUID format, required).")]

❌ DON'T:
[Description("The user ID.")]
```

### 5. Use Consistent Operation Naming
```csharp
✅ DO:
[EndpointName("GetUserById")]
[EndpointName("CreateUser")]
[EndpointName("UpdateUser")]
[EndpointName("DeleteUser")]

❌ DON'T:
[EndpointName("GetUser")]
[EndpointName("Add")]
[EndpointName("Modify")]
[EndpointName("Remove")]
```

---

## OpenAPI Output Examples

### Example 1: GET with Parameter Description
```json
{
  "get": {
    "summary": "Get a greeting",
    "description": "Returns a personalised greeting for the name in the requested language.",
    "operationId": "GetGreeting",
    "tags": [
      { "$ref": "#/components/tags/greetings" },
      { "$ref": "#/components/tags/retrieval" }
    ],
    "parameters": [
      {
        "name": "name",
        "in": "path",
        "description": "The name to include in the greeting (1-50 characters).",
        "required": true
      }
    ],
    "responses": {
      "200": { "description": "Success" },
      "400": { "description": "The name parameter is invalid." },
      "404": { "description": "The greeting does not exist." }
    }
  }
}
```

### Example 2: POST with Request Body
```json
{
  "post": {
    "summary": "Create a greeting",
    "description": "Returns a greeting for the name and language provided in the request body.",
    "operationId": "CreateGreeting",
    "tags": [
      { "$ref": "#/components/tags/greetings" },
      { "$ref": "#/components/tags/creation" }
    ],
    "requestBody": {
      "required": true,
      "content": {
        "application/json": {}
      }
    },
    "responses": {
      "200": { "description": "Success" },
      "400": { "description": "The request body failed validation." },
      "422": { "description": "The request body is malformed." }
    }
  }
}
```

### Example 3: DELETE with No Content Response
```json
{
  "delete": {
    "summary": "Delete a greeting",
    "description": "Removes a greeting from the system.",
    "operationId": "DeleteGreeting",
    "tags": [
      { "$ref": "#/components/tags/greetings" },
      { "$ref": "#/components/tags/deletion" }
    ],
    "parameters": [
      {
        "name": "name",
        "in": "path",
        "description": "The name of the greeting to delete.",
        "required": true
      }
    ],
    "responses": {
      "204": { "description": "The greeting was successfully deleted." },
      "400": { "description": "The name parameter is invalid." },
      "404": { "description": "The greeting does not exist." }
    }
  }
}
```

---

## Common Mistakes to Avoid

### ❌ Mistake 1: Forgetting [Consumes] for POST/PUT
```csharp
❌ WRONG:
[Function("CreateUser")]
public static IActionResult Create(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "users")]
    HttpRequest req,
    [FromBody] CreateUserRequest request)

✅ CORRECT:
[Consumes("application/json")]
[Function("CreateUser")]
public static IActionResult Create(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "users")]
    HttpRequest req,
    [FromBody] CreateUserRequest request)
```

### ❌ Mistake 2: Not Documenting All Response Codes
```csharp
❌ WRONG:
[ProducesResponseType<User>(StatusCodes.Status200OK)]

✅ CORRECT:
[ProducesResponseType<User>(StatusCodes.Status200OK)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
```

### ❌ Mistake 3: Missing Parameter Descriptions
```csharp
❌ WRONG:
string id

✅ CORRECT:
[Description("The user ID (UUID format).")]
string id
```

### ❌ Mistake 4: Single Generic Tags
```csharp
❌ WRONG:
[Tags(["api"])]

✅ CORRECT:
[Tags(["users", "retrieval"])]
```

---

## Where to Find Reference Material

- **Microsoft Learn Documentation:**  
  https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/include-metadata?view=aspnetcore-10.0

- **ASP.NET Core OpenAPI Overview:**  
  https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/

- **OpenAPI Specification:**  
  https://spec.openapis.org/oas/v3.1.0

---

## Summary

Use these attributes to create well-documented Azure Functions APIs:

1. `[EndpointSummary]` + `[EndpointDescription]` → Clear operation documentation
2. `[Tags]` → Organize endpoints logically
3. `[EndpointName]` → Consistent operation IDs
4. `[Consumes]` → Document request format
5. `[ProducesResponseType]` → Document all response codes
6. `[Description]` → Parameter documentation
7. `[Obsolete]` → Mark deprecated endpoints
8. `[ApiExplorerSettings]` → Hide internal endpoints

**Result:** Professional, production-ready API documentation automatically generated from your code!
