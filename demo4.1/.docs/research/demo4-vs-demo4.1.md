# Demo4 vs Demo4.1 Discrepancy Summary

## Purpose

- Demo4 strengthens the monolithic Blazor experience by combining local passkey and Microsoft Entra ID authentication with permission-based authorization and Graph OBO calls, ensuring the hybrid identity story remains rooted in the demo3 pipeline before adding Entra-specific claims mapping and diagnostics.[demo4/README.md](demo4/README.md#L5-L90)
- Demo4.1 reframes that foundation through a BFF + YARP proxy fronted by .NET Aspire, emphasizing secure downstream API calls and InteractiveAuto rendering for the distributed setup.[demo4.1/README.md](demo4.1/README.md#L5-L55)

## Architectural and Pattern Differences

- Demo4 still serves a single Blazor Web App that wires the hybrid authentication sources through `IClaimsTransformation`, retains cookie-based BFF endpoints, and keeps Graph access on the server side, illustrating incremental enrichment of the earlier monolith.[demo4/README.md](demo4/README.md#L38-L78)
- Demo4.1 splits responsibilities across `SaaS.Frontend`, `SaaS.Backend`, `SaaS.AppHost`, and `SaaS.Frontend.Client`, layering a YARP reverse proxy, Aspire orchestration, and downstream OBO flows so that the BFF handles auth while the backend exposes protected APIs.[demo4.1/README.md](demo4.1/README.md#L13-L55)

## Progressive Enhancements

- The “What’s New” list for Demo4 highlights Entra ID authentication, Graph integration, authentication-state serialization, claims bridging, and enhanced diagnostics, all framed within the monolithic app surface.[demo4/README.md](demo4/README.md#L79-L90)
- Demo4.1’s “What’s New” narrows the scope to distributed concerns: onboarding .NET Aspire service discovery, adding the YARP proxy for `/api/proxy/weather/{**catch-all}`, and refining OBO token handling before delegating to downstream APIs.[demo4.1/README.md](demo4.1/README.md#L56-L62)


## Identity validation findings

- The comparison doc claims demo4 keeps the local passkey + Entra story alive with explicit claims bridging (`IClaimsTransformation`) and hybrid authorization, but demo4.1’s BFF only wires Microsoft Identity Web via `.AddMicrosoftIdentityWebApp(...)` and downstream token acquisition for Weather/Graph so there is no local identity/passkey stack or claims transformation code to reconcile Entra and local accounts.[demo4.1/.docs/research/demo4-vs-demo4.1.md](demo4.1/.docs/research/demo4-vs-demo4.1.md#L4-L10); [demo4.1/SaaS.Frontend/Program.cs](demo4.1/SaaS.Frontend/Program.cs#L16-L134)
- The backend merely trusts Entra-issued JWTs (authority `https://login.microsoftonline.com/{tenant}/v2.0`, scope `access_as_user`) and applies scope validation — there are no registers of `IdentityUser`, EF Core stores, or claim-mapping handlers that could auto-provision/link Entra to a local account.[demo4.1/SaaS.Backend/Program.cs](demo4.1/SaaS.Backend/Program.cs#L14-L124)
- The UI surface is also locked to the Microsoft Identity UI sign-in/sign-out endpoints, so there are no alternate local/passkey login routes for clients to hit; only the Entra paths exist today.[demo4.1/SaaS.Frontend/Components/Layout/NavMenu.razor](demo4.1/SaaS.Frontend/Components/Layout/NavMenu.razor#L1-L44)
- A workspace-wide search for `IClaimsTransformation`, `passkey`, or `IdentityUser` only returns this research note, which means no production code currently implements the claims mapping or auto-provisioning/account linking the comparison doc references.[demo4.1/.docs/research/demo4-vs-demo4.1.md](demo4.1/.docs/research/demo4-vs-demo4.1.md#L4-L10)

## Operational and Configuration Notes

- Demo4 still requires applying EF Core migrations and launching the single Entra integration project via `dotnet watch`, followed by the verification checklist that exercises passkeys, Entra logins, Graph data, and permissions in the monolith.[demo4/README.md](demo4/README.md#L93-L113)
- Demo4.1 instead runs via `dotnet run --project SaaS.AppHost --launch-profile https`, documents tenant/app registration requirements, and reproduces detailed Entra settings plus downstream API scopes for the proxy-backed flow.[demo4.1/README.md](demo4.1/README.md#L66-L107)
