using System.Collections.Generic;

namespace DProcess.Shared.Dto;

public sealed record UserPermissionsDto(
    string UserId,
    string Email,
    IReadOnlyList<string> Permissions);