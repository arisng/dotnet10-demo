# Tasks: Microsoft Graph Profile Synchronization

1.  **Task 1**: Add properties `Department`, `OfficeLocation`, `MobilePhone`, and `LastGraphSync` (DateTimeOffset?) to `demo4/Demo4.EntraIntegration/Data/ApplicationUser.cs`.
2.  **Task 2**: Create a new Entity Framework migration named `AddGraphProfileFields` and apply it to the database.
3.  **Task 3**: Update `IGraphService` interface and `GraphService` implementation in `demo4/Demo4.EntraIntegration/Services/` to include `SyncUserProfileToLocalAsync(string userId)`.
4.  **Task 4**: Modify `EntraUserProvisioningService.cs` in `demo4/Demo4.EntraIntegration/Services/` to call `IGraphService.SyncUserProfileToLocalAsync` within the user update/creation flow.
5.  **Task 5**: Update `AuthStateSurface.razor` in `demo4/Demo4.EntraIntegration.Client/Components/Diagnostics/` to display the newly added profile fields.
6.  **Task 6**: Perform manual verification (or automated if feasible) of the profile sync flow.
7.  **Task 7**: Update `demo4/README.md` to reflect the completed implementation.
