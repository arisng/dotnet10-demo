# Research: The "user_null" Error and the Identity-Entra Claims Bridge

## 1. The Context: A Hybrid Authentication Story
In this workshop, we use two authentication systems living together:
1.  **ASP.NET Core Identity**: Manages your local session (stored in a cookie called `.AspNetCore.Identity.Application`).
2.  **Microsoft Entra ID**: An external service that gives you a token to call APIs like Microsoft Graph.

## 2. The Problem: The Disconnected Cookie
When you log in via Entra ID, the app creates a local user in the database and signs you in using **Identity**. 

The **Identity Cookie** is very simple. Its job is to remember your ID and Email. It does **not** automatically remember the complex keys (`uid`, `utid`, `msal_account_id`) that Microsoft's library needs to find your API tokens.

### The "user_null" Failure
When you visit the Profile page:
1.  The app looks at your **Identity Cookie**. It sees "User: John Doe".
2.  The app calls Microsoft Graph and asks for a token for "John Doe".
3.  Microsoft's library looks at the "John Doe" principal and says: *"I don't see any Entra ID keys here. I have no idea which Microsoft account this belongs to!"*
4.  **Result**: It throws an error called `MsalUiRequiredException` with the code `user_null`.

## 3. The Infinite Loop Explained
Because of the error above, our "API-to-Navigation Handoff" pattern kicks in:
1.  The App says: "Go to Microsoft and get a token!"
2.  The Browser goes to Microsoft.
3.  Microsoft says: "You are already logged in! Here is your identity."
4.  The Browser comes back to the App.
5.  **The Flaw**: The App logs you in again, but **still only gives you a lean Identity Cookie.**
6.  You go back to the Profile page, and the cycle repeats forever.

## 4. The Solution: The Claims Bridge
To fix this, we must "enrich" the identity of the user for every single request. 

### Implementation: `IClaimsTransformation`
We use a special middleware called `PermissionClaimsTransformation`. Whenever a request comes in, we will:
1.  Check if the user is an Entra user (by looking at their `EntraObjectId` in the database).
2.  If yes, **manually inject** the missing keys into the current request's identity:
    *   `uid` (Unique ID)
    *   `utid` (Unique Tenant ID)
    *   `msal_account_id` (The combined key)

### Why this works:
By adding these "hints" to the user's identity on the fly, Microsoft's library can now reliably find the API tokens in its cache, even if the actual cookie doesn't store them permanently.

## 5. Technical Mechanics (For Developers)
| Component                  | Responsibility                                                                                       |
| -------------------------- | ---------------------------------------------------------------------------------------------------- |
| **Database**               | Stores the durable `EntraObjectId`.                                                                  |
| **Claims Transformation**  | Runs on every request. Reads the DB and adds `uid`/`utid` claims to the `ClaimsPrincipal` in memory. |
| **Microsoft Identity Web** | Uses the new `uid`/`utid` claims as a lookup key in the token cache.                                 |
| **Graph Service**          | Successfully acquires the token silently. **End of Infinite Loop.**                                  |
