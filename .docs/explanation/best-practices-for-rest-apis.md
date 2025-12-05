# Best Practices for Building REST APIs

## Understanding REST Maturity Levels

Leonard Richardson's maturity model defines four levels of REST adoption:

### Level 0: The Swamp of POX
- Uses HTTP as transport for RPC (Remote Procedure Call)
- Single endpoint (e.g., `/api/service`) handling all operations via POST
- Request body contains operation details
- Common in SOAP APIs

### Level 1: Resources
- Introduces multiple resource-based URIs
- Separates endpoints by resource type (e.g., `/api/orders`, `/api/customers`)
- Still lacks proper HTTP method usage

### Level 2: HTTP Verbs
- Correctly uses HTTP methods: GET, POST, PUT, DELETE
- Applies meaningful status codes (200, 201, 404, etc.)
- Most production APIs operate at this level
- Predictable and follows HTTP standards

### Level 3: Hypermedia Controls (HATEOAS)
- Responses include links to related resources and available actions
- Clients discover API capabilities through responses
- True REST, but often overkill for many applications

## Resource Naming Principles

### Use Nouns, Not Verbs
Resources represent entities, not actions. HTTP methods indicate operations:

- ✅ `/api/products` (noun)
- ❌ `/api/getProducts` (verb)

### Plural Nouns for Collections
Collections use plural forms for consistency:

- ✅ `/api/products`, `/api/orders`
- Even for single items: `/api/products/{id}`

### Multi-Word Resources
Adopt kebab-case (lowercase with hyphens) for readability and URL-friendliness:

- ✅ `/api/product-categories`, `/api/shopping-carts`
- Avoid camelCase or PascalCase in URLs

### Resource Hierarchies and Nesting
Reflect parent-child relationships in URLs, but limit to two levels:

- ✅ `/api/orders/{orderId}/items`
- ❌ Deep nesting like `/api/customers/{id}/orders/{id}/items/{id}/reviews/{id}`

For deeply nested resources, provide top-level access:

- `/api/reviews/{reviewId}` (direct access)

### Query Parameters for Filtering
Use query strings for resource filtering, not actions:

- ✅ `/api/products?category=electronics&inStock=true`
- ❌ `/api/products?action=filter&category=electronics`

## HTTP Methods Usage

### GET - Retrieve Resources
- Safe and idempotent
- Never modifies data
- Can be cached
- Returns 200 OK or 404 Not Found

### POST - Create New Resources
- Creates new resources
- Not idempotent (multiple calls create multiple resources)
- Returns 201 Created with Location header
- Includes created resource in response body

### PUT - Replace Entire Resource
- Replaces complete resource with provided data
- Idempotent
- Requires all fields in request
- Returns 200 OK or 204 No Content

### PATCH - Partial Update
- Updates only specified fields
- Idempotent when designed properly
- Uses nullable types for optional fields
- Returns 204 No Content

### DELETE - Remove Resources
- Removes resource
- Idempotent (deleting twice has same effect)
- Returns 204 No Content or 404 if not found

## Status Codes

### Success Codes
- **200 OK**: Standard success for GET, PUT, PATCH
- **201 Created**: Resource successfully created (POST)
- **202 Accepted**: Request accepted for asynchronous processing
- **204 No Content**: Success with no response body (DELETE, updates)

### Client Error Codes
- **400 Bad Request**: Malformed request or invalid data
- **401 Unauthorized**: Missing or invalid authentication
- **403 Forbidden**: Authenticated but insufficient permissions
- **404 Not Found**: Resource does not exist
- **409 Conflict**: Request conflicts with current resource state
- **422 Unprocessable Entity**: Valid request but fails business validation
- **429 Too Many Requests**: Rate limiting triggered

### Server Error Codes
- **500 Internal Server Error**: Unexpected server-side error
- **503 Service Unavailable**: Dependent service unavailable

## API Versioning Strategies

### URI Versioning (Recommended)
- Includes version in URL path: `/api/v1/products`
- Immediately visible and easy to test
- Simple routing in gateways
- Clear for documentation

### Header Versioning
- Uses custom header: `X-Api-Version: 2.0`
- Keeps URLs clean but harder to test
- Easy to forget header

### Media Type Versioning
- Uses Accept header: `Accept: application/vnd.myapi.v2+json`
- Follows REST principles closely
- More complex to implement and use

### Query String Versioning (Avoid)
- Uses query parameter: `/api/products?version=2`
- Mixes versioning with filtering
- Easy to omit accidentally

## Request and Response Standards

### JSON as Standard Format
- Human-readable and language-agnostic
- Universally supported

### Consistent Property Naming
- Use camelCase for JSON properties
- Apply throughout entire API

### Error Response Standardization
- Implement RFC 9457 (Problem Details) for consistent error format
- Include type, title, status, detail, instance
- Extend for validation errors with field-specific messages

## Why These Practices Matter

These guidelines create predictable, maintainable APIs that developers can understand without extensive documentation. They follow HTTP standards, enabling proper client behavior and tool integration. While Level 3 HATEOAS represents true REST, Level 2 provides practical benefits for most applications.

For practical implementation guides, see how-to articles on building REST APIs with ASP.NET Core. For technical reference, consult the ASP.NET Core API documentation.