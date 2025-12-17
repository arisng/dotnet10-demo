# Demo5 Cannot Be Tested Due to Missing Entra ID App Registrations

**Date:** 2025-12-07
**Type:** Bug
**Severity:** High
**Status:** Investigating

---

## Problem

Demo5 cannot be tested because the Entra ID app registrations required for the downstream API integration are not configured. The `appsettings.json` files contain placeholders that need to be replaced with actual Azure app registration values, preventing the OBO (On-Behalf-Of) flow from functioning and causing downstream API calls to fail.

## Root Cause

The root cause is the absence of proper Azure Entra ID setup. Demo5 requires two app registrations: one for the Blazor client (inherited from demo4) and a new one for the protected API. Without these, authentication and authorization for the downstream API cannot proceed.

## Solution

Follow the "Entra ID Configuration" steps outlined in `demo5/README.md`:

1. Create a new app registration for the Protected API in Azure Portal.
2. Expose the API with the required scope (`Forecast.Read`).
3. Grant permissions from the Blazor app registration to the Protected API.
4. Update the `appsettings.json` files in `Demo5.DownstreamApi` and `Demo5.ProtectedApi` with the actual client IDs, tenant IDs, and scopes.

## Lessons Learned

- Ensure all external dependencies, such as Azure app registrations, are configured before attempting to run or test demos involving third-party integrations.

## Prevention

- [ ] Verify Entra ID app registrations are created and configured prior to demo development.
- [ ] Include setup checklists in README files for demos requiring external services.

**Tags:** bug identity entra-id dotnet
