# Issue: Entra Profile "Profile not available" due to missing Identity UI and broken challenge flow

## Status
- **Type**: Bug
- **Priority**: High
- **Created**: 2026-01-20
- **Status**: Resolved

## Description
When accessing the `/entra-profile` page, users initially received a "Profile not available" warning. Behind the scenes, the browser console showed a **404** when trying to redirect to the authentication challenge, and the server logs showed a **500** error caused by `MsalUiRequiredException`.

## Root Causes (The "Why")

### 1. Missing Infrastructure (The 404 Error)
When our code needs a new permission (like "Read Profile from Microsoft Graph"), it tries to send the user to a special "Challenge" page at `/MicrosoftIdentity/Account/Challenge`.
*   **The Problem**: This page is "virtual"—it's provided by a specific Microsoft library that wasn't installed, and its routes weren't mapped in our application.

### 2. The "Broken Chain" of Authentication (The 500 Error)
When a user logs in via the standard "Login" button, they are authenticated into the **website**, but they haven't given permission for the website to talk to **Microsoft Graph** on their behalf.
*   **The Problem**: The app tries to get a "Graph Token" silently. Because the user hasn't explicitly consented to the `User.Read` scope yet, Microsoft's libraries throw an `MsalUiRequiredException` (essentially saying "I need to talk to the user first").

### 3. Scheme Name Conflict (The "Silent Killer")
The virtual controllers in the Microsoft Identity library expect the authentication system to be named `OpenIdConnect`.
*   **The Problem**: We had named our scheme `MicrosoftEntra`. This worked for basic login but broke the "Challenge" system because the library couldn't find its own configuration under the custom name.

## Internal Mechanics: How it works (for Junior Devs)

1.  **Identity Cookie ≠ API Token**: When you log in, you get a cookie that says "I am User X". This cookie does **not** contain the power to call APIs.
2.  **Incremental Consent**: Instead of asking for every possible permission at the start (which scares users), we wait until they visit a page that *needs* those permissions (like the Profile page).
3.  **The Handoff**: 
    *   App calls Graph → MSAL says "I don't have a token" (`MsalUiRequiredException`).
    *   The App catches this and says "Redirect user to the Challenge page".
    *   The Challenge page says "Redirect user to Microsoft Login for permission".
    *   User clicks "Accept" → Browser returns to App → App now has the token.

## Implementation Details (The Solution)

### 1. Installing the "UI Engine"
We added the `Microsoft.Identity.Web.UI` NuGet package. This package contains the hidden controllers that handle the `/MicrosoftIdentity/*` paths.

### 2. Mapping the Routes in `Program.cs`
We had to tell ASP.NET Core specifically to look for these controllers:
```csharp
builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

// ... later in the file ...

app.MapControllers(); 
```

### 3. Standardizing the Scheme Name
We changed the authentication scheme name from `MicrosoftEntra` back to the industry standard `OpenIdConnectDefaults.AuthenticationScheme` (which is just the string `"OpenIdConnect"`). This ensured that the Microsoft Identity libraries and our app were "speaking the same language."

## Verification Path
1.  User visits `/entra-profile`.
2.  App throws `MsalUiRequiredException`.
3.  App redirects to `/MicrosoftIdentity/Account/Challenge` (**Verified: No longer 404s**).
4.  User is sent to `login.microsoftonline.com` to grant permission.
5.  User returns and sees their profile data.

## Phase 2: CORS Error in WASM (Resolved)
After fixing the 404, we faced a **CORS error** because the browser blocked the `fetch` redirect to Microsoft.
*   **Resolution**: Implemented the **API-to-Navigation Handoff** pattern.
*   **Mechanic**: Server returns 403 + Header -> Client detects Header -> Client does `forceLoad: true` navigation to the challenge endpoint.
*   **Verification**: Tested in WASM mode; the browser now successfully performs a full-page redirect to Microsoft for consent.

## Next Steps
- [x] Install `Microsoft.Identity.Web.UI` package.
- [x] Update `Program.cs` with controllers and endpoint mapping.
- [x] Rename all occurrences of `MicrosoftEntra` to standard `OpenIdConnect`.
- [x] Implement API-to-Navigation Handoff to bypass CORS.
- [x] Verify the full interactive challenge flow in both Server and WASM modes.
