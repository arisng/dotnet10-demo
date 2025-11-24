# Research: Account Linking and Migration in ASP.NET Core Identity v3 (.NET 10) - 2025-11-24

## Context

**Requested by:** Conductor-Agent  
**Target:** demo4 (EntraIntegration)  
**Goal:** Inform implementation plan for handling duplicate emails during Entra ID login (linking local users to Entra accounts).

## Key Findings

### 1. .NET 10 Identity v3 Features ✅

- **Source:** Microsoft Learn (ASP.NET Core Identity docs, Social Authentication docs)  
- **Version:** .NET 10.0  
- **Status:** Stable, no major changes from .NET 8/9  
- **Key Points:**
  - Identity v3 remains the core framework for user management, passwords, roles, claims, and external logins.
  - No built-in "auto-migration" features; external providers (including Entra ID) are handled via standard OAuth/OIDC flows.
  - Entra ID is treated as a standard external login provider (Microsoft Account), not special-cased.
  - Claims transformation via `IClaimsTransformation` supports merging/normalizing claims from multiple sources.
  - Metrics added for monitoring identity operations (new in recent versions, available in .NET 10).

### 2. Architectural Patterns for Account Linking

- **Manual Linking (Safest, Recommended for Security):**
  - Users link providers themselves via Manage > External Logins UI.
  - Use `UserManager.AddLoginAsync()` to associate external login with existing account.
  - Prevents takeover; requires user consent.
- **Prompted Resolution (Balanced for UX):**
  - On duplicate email, redirect to custom UI for user choice: link existing account (with password verification), create new, or cancel.
  - Validates security while enabling migration.
- **Auto-Linking (Risky, Not Recommended):**
  - Automatically link on duplicate; high risk of account takeover without verification.
- **Claims Handling:**
  - Implement `IClaimsTransformation` to merge claims (e.g., Entra ID `oid` with local user ID).
  - Use `UserManager.ReplaceClaimAsync()` for updates during linking.

### 3. Best Practices for Entra ID Integration 🔒

- **Security Considerations:**
  - Always require local password or 2FA before linking to prevent takeover.
  - Log all linking/migration actions with user ID, email, provider for audit.
  - Use HTTPS; validate tokens properly.
  - Mitigate DoS with rate limiting on failed link attempts.
- **Implementation Patterns:**
  - Detect Entra users via `GetObjectId()` (oid claim).
  - For duplicates: Query existing user by email, prompt for resolution.
  - Update user fields (e.g., `EntraObjectId`) on successful link.
  - Sync profile via Microsoft Graph API (using Microsoft.Identity.Web).
- **Error Handling:** Use Identity error descriptors (e.g., `LoginAlreadyAssociated`); avoid exposing sensitive details.

### 4. NuGet Packages and Extensions

- **Core Packages (Already in Workspace):**
  - `Microsoft.AspNetCore.Identity.EntityFrameworkCore` v10.0.0: Essential for EF Core integration, external logins.
  - `Microsoft.Identity.Web` v4.1.0: Simplifies Entra ID auth/authorization, Graph API calls.
  - `Microsoft.AspNetCore.Authentication.JwtBearer` v10.0.0: For API token validation.
- **Community Options:**
  - Duende IdentityServer: Advanced for enterprise federation (commercial license).
  - OpenIddict: Flexible OAuth/OIDC stack.
  - CoreIdent: Developer-friendly alternative.
- **Recommendation:** Stick to official Microsoft packages for .NET 10 compatibility and support.

### 5. Common Pitfalls ⚠️

- Assuming auto-linking is safe—always verify user intent.
- Overwriting local claims without merging logic.
- Not handling Entra-specific claims (e.g., `oid`) properly.
- Missing logging/auditing for compliance.
- Using outdated patterns from .NET Core 3.1 (e.g., no `RequireUniqueEmail` enforcement).

## Recommendations for Implementation

**Architecture Decision:**  
Implement **prompted resolution** for demo4: On `CreateAsync` failure due to duplicate email, log warning, find existing user, and redirect to a linking page with password confirmation. This balances security (prevents takeover) with UX (enables migration), aligning with incremental demo goals.

**Code Changes Required:**

1. Modify `CreateEntraUserAsync` to catch duplicate errors and return existing user info.
2. Add custom exception (e.g., `AccountLinkRequiredException`) for controller handling.
3. Create linking UI/page (scaffold from Identity UI).
4. Update `PermissionClaimsTransformation` to handle linked users.
5. Add logging and validation.

**Testing Strategy:**

- Unit tests for duplicate handling.
- Integration tests for Entra login flow.
- Security tests for takeover prevention.
- Load tests for performance.

**Documentation Updates:**

- Update demo4 README.md: Add "What's New" section on account linking/migration feature.
- Include code examples and security notes.

## References

- [ASP.NET Core Identity Overview](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity)
- [External Provider Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/)
- [Microsoft.Identity.Web Docs](https://learn.microsoft.com/en-us/azure/active-directory/develop/microsoft-identity-web)
- NuGet Package Analysis (via Context7-Agent)
