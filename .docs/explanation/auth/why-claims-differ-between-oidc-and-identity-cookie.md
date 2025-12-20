# Why Claims Differ Between `OnTokenValidated` and Later Requests (demo4)

It’s normal to observe more claims in the OIDC callback (`OnTokenValidated`) than you see later in request handling or Blazor authentication state.

## What’s happening

- `OnTokenValidated` runs during the OpenID Connect callback, with the raw principal produced from the identity token (and sometimes userinfo/app role mapping).
- After sign-in, the app typically issues an **ASP.NET Core Identity cookie** that becomes the durable session representation.
- The cookie principal can be smaller and different:
  - some claims are not persisted
  - some claims are mapped/renamed
  - some claims are filtered for size/safety

## Why this matters in Blazor InteractiveAuto

In `InteractiveAuto`, the app transitions from server-rendered HTML to WASM execution. To prevent extra round-trips, the app persists a snapshot of auth state (`UserInfo`) into `PersistentComponentState`.

If Entra markers (`oid`, `tid`) are not present in the server-side principal at persistence time (or not carried into the persisted snapshot), the browser-side principal will not satisfy Entra-only policies.

## Practical implication

If a claim must be available later (for policies, UI gating, downstream API calls), make it durable:

- Persist it into the Identity user store as an Identity user claim (so it can be reconstructed into the cookie principal)
- Or explicitly include it in SSR → WASM persisted state

## Related

- OIDC event: `demo4/Demo4.EntraIntegration/Program.cs`
- Entra provisioning: `demo4/Demo4.EntraIntegration/Services/EntraUserProvisioningService.cs`
- Auth persistence: `demo4/Demo4.EntraIntegration/Authorization/PersistingServerAuthenticationStateProvider.cs`
