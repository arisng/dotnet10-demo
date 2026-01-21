# Research: Solving CORS Errors in Blazor WASM with the "API-to-Navigation Handoff" Pattern

## 1. The Problem: "The Forbidden Redirect"

When using **Blazor InteractiveAuto** (WASM mode), the browser makes a `fetch` (asynchronous) request to your server's API to get data (e.g., `/api/graph/profile`).

If that API returns a **302 Redirect** to an external site (like `login.microsoftonline.com` for a login challenge):
1.  The browser's `fetch` engine tries to follow the redirect in the background.
2.  Because the destination is a different domain (`microsoftonline.com`), the browser checks for **CORS** (Cross-Origin Resource Sharing) headers.
3.  Microsoft's login page (correctly) does not allow being loaded via background `fetch` requests for security reasons.
4.  **Result**: The browser blocks the request, and you see a **CORS error** in the console.

## 2. The Solution: API-to-Navigation Handoff

Instead of letting the server redirect the *background request*, we tell the *UI* to do a *foreground navigation*.

### The Pattern Flow:
1.  **API Level**: When a challenge is needed (incremental consent), the server returns a `403 Forbidden` with a special custom header (e.g., `x-ms-challenge-required`).
2.  **Client Service**: The WASM service detects this header and throws a specific exception (`ChallengeRequiredException`).
3.  **UI Component**: The Razor page catches this exception and uses `NavigationManager` with `forceLoad: true` to perform a full browser refresh to the challenge endpoint.
4.  **Handoff**: Since it's now a full page navigation, the browser handles the redirect to Microsoft normally without CORS restrictions.

---

## 3. Implementation Breakdown (Step-by-Step)

### A. The Shared Signal ([Shared Project])
We created a custom exception in the `.Shared` project so that both the Server and Client can "speak the same language" without needing complex dependencies.
*   **File**: `Demo4.EntraIntegration.Shared/Models/ChallengeRequiredException.cs`

### B. The Server-Side Guard ([Server Project])
The `GraphService` on the server catches the Microsoft-specific exception and wraps it in our shared exception.
*   **File**: `Demo4.EntraIntegration/Services/GraphService.cs`

The API endpoint then catches that shared exception and converts it into a CORS-safe response.
*   **File**: `Demo4.EntraIntegration/Program.cs`
*   **Action**: Return `403 Forbidden` + `x-ms-challenge-required: true` header.

### C. The Client-Side Detection ([Client Project])
The WASM service checks every response for that secret header. If found, it "blows up" with the special exception.
*   **File**: `Demo4.EntraIntegration.Client/Services/ClientServices.cs`

### D. The UI Reaction ([Client Project])
The Profile page wraps its loading logic in a `try/catch`. If it sees the "Challenge required" signal, it kicks the user to the full-page login flow.
*   **File**: `Demo4.EntraIntegration.Client/Components/Pages/Profile.razor`
*   **Key Code**: `NavigationManager.NavigateTo("MicrosoftIdentity/Account/Challenge", forceLoad: true)`

## 4. Why `forceLoad: true`?
By default, `NavigationManager.NavigateTo` just changes the URL and tries to render internally without refreshing (Client-side routing).
By setting `forceLoad: true`, we tell the browser: *"Stop what you're doing, and actually load this URL from the server as if I typed it in the address bar."* This is what allows the server-side redirect to `microsoftonline.com` to work.

## 5. Junior Dev Cheat Sheet
| Phase              | Action                                         | Why?                                                           |
| ------------------ | ---------------------------------------------- | -------------------------------------------------------------- |
| **Server**         | Catch Ex -> Return 403 + Header                | Stop the "Illegal Redirect" before it hits the browser.        |
| **Client Service** | Header Found -> Throw Ex                       | Signal to the UI that we can't continue silently.              |
| **Razor UI**       | Catch Ex -> `NavigateTo(..., forceLoad: true)` | Switch from a background "Fetch" to a foreground "Navigation". |
