# Passkey Authentication (WebAuthn)


**Introduced:** demo2  
**Category:** Authentication  
**Complexity:** ⭐⭐ (Intermediate)

**Definition:**
Passwordless authentication using the WebAuthn API. Users register a security key or biometric, then authenticate by proving possession of the private key.

**Use Cases:**
- Consumer-facing applications requiring high security
- Organizations reducing password-related breaches
- Multi-platform authentication (desktop, mobile, web)
- Compliance-driven environments (financial services, healthcare)

**Implementation Details:**
- ASP.NET Core Identity with IdentitySchemaVersion3
- `.NET 10`: New `MapAdditionalIdentityEndpoints()` wires `/PasskeyCreationOptions` and `/PasskeyRequestOptions`
- Full Manage UI components: `Passkeys.razor`, `RenamePasskey.razor`
- Credential registration and assertion validation handled by framework

**Strengths:**
- ✅ Stronger security than passwords
- ✅ Better UX (biometric/device unlock)
- ✅ Phishing-resistant
- ✅ Cross-platform support
- ✅ Built-in .NET 10 support

**Weaknesses:**
- ❌ Requires WebAuthn-capable browser/device
- ❌ Learning curve for users
- ❌ Recovery procedures needed if key lost

**Related Patterns:**
- Multi-Identity
- [Claims Transformation](authz-claims-transformation.md)

**Demo References:**
- demo2: Complete passkey implementation and diagnostics
- demo3: Passkey users assigned roles/permissions
- demo4: Passkeys coexist with Entra ID
- demo6: Per-tenant passkey/Entra toggle

