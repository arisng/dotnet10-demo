# Plan: Implement Microsoft Graph Profile Synchronization for Demo4

This plan addresses issue 260119, filling the gap between documented behavior and actual functionality regarding user profile synchronization from Microsoft Graph API.

## Core Objectives
1.  **Data Model Enhancement**: Update `ApplicationUser` to store additional profile fields from Entra ID.
2.  **Service Implementation**: Implement the logic to fetch and map Graph profile data to the local user record.
3.  **Pipeline Integration**: Ensure synchronization happens automatically during the Entra ID login flow.
4.  **UI Feedback**: Display the synced data in the application's diagnostic and profile views.
5.  **Validation**: Verify the flow and update project documentation.

## Technical Details
- Project: `demo4/Demo4.EntraIntegration`
- Base Pattern: .NET Identity + Entra ID + Microsoft Graph
- Strategy: Server-side sync during user provisioning.
