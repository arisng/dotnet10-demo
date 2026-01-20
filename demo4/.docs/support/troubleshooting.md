# Demo4 Troubleshooting Playbook

## 1. "Sign in with Microsoft" button missing
1. Run `dotnet user-secrets list` to confirm `AzureAd` config values exist.
2. Double-check the `appsettings.json` AzureAd section (copy the snippet from `reference/quick-reference.md`).
3. Make sure the redirect URIs in Entra match `https://localhost:7210/signin-oidc` and `/signout-callback-oidc` exactly.
4. Restart the app after configuration changes.

## 2. AADSTS50011: Reply URL mismatch
- Compare portal redirect URIs with the app’s callback path (`/signin-oidc`).
- Avoid mixing https vs http; the Azure redirect must match the protocol and port.
- Reload the app and sign-in page after editing `appsettings.json`.

## 3. AADSTS65001: Need admin consent
- Visit the App Registration → API permissions and click "Grant admin consent".
- Re-run the sign-in; the consent prompt should disappear after granting.

## 4. Graph API 401 or profile data missing
- Verify `DownstreamApi:Scopes` (must include `User.Read`).
- Ensure `EnableTokenAcquisitionToCallDownstreamApi()` and the `GraphService` are registered.
- Check for MSAL messages in the logs (`MsalUiRequiredException`, `MsalServiceException`).

## 5. Entra user has no permissions
- Newly provisioned Entra users have zero roles—assign them via the SQL snippets in `reference/quick-reference.md` or wait for Demo6 automation.
- Confirm the user appears in `AspNetUserRoles`.
- Refresh `https://localhost:7210/auth-state-probe` to reload permission claims.

## 6. Token cache resets or login loops
- In-memory caches are development-only. Switch to Redis/SQL with `.AddDistributedTokenCaches()` and enable encryption (`MsalDistributedTokenCacheAdapterOptions.Encrypt = true`).
- Use `AddDataProtection()` to persist keys across instances.
- Clear cookies or restart the app to wipe the in-memory cache after config changes.

## 7. Claims transformation re-running
- The `permission_transformed` claim prevents duplication; ensure the code checks for it before adding permissions.
- Add structured logs around `PermissionClaimsTransformation.TransformAsync` to see when it fires.
- If the claim is missing, inspect the auth pipeline to confirm the transformation middleware runs only once per request.

## 8. External login provider woes
- Always store the Entra `oid` as both `EntraObjectId` and `AspNetUserLogins.ProviderKey` (the `AUTO_PROVISIONING_RESEARCH.md` doc explains why).
- Use diagnostics in `PermissionClaimsTransformation` to log `oid`, `sub`, and `GetNameIdentifierId()` for a given login.
- If provisioning flagged a failure, delete the stale user before retrying to avoid conflicts.

## Quick diagnostics
- Watch for `[PermissionClaimsTransformation]`, `[GraphService]`, and `MSAL` logs for immediate clues.
- Use `dotnet watch` console output to view `aspnetcore.authentication.*` and `aspnetcore.authorization.*` metrics (enabled via OpenTelemetry in `Program.cs`).
- When in doubt, start from `reference/quick-reference.md` and follow the verification checklist in `guidance/setup-guide.md`.