using System;
using System.Collections.Generic;

namespace DProcess.Shared.Permissions;

public static class PermissionNames
{
    public const string WeatherRead = "weather.read";
    public const string WeatherWrite = "weather.write";

    public const string UsersRead = "users.read";
    public const string UsersWrite = "users.write";
    public const string UsersDelete = "users.delete";

    public const string ReportsView = "reports.view";
    public const string ReportsExport = "reports.export";

    private static readonly string[] AllPermissionsArray =
    {
        WeatherRead,
        WeatherWrite,
        UsersRead,
        UsersWrite,
        UsersDelete,
        ReportsView,
        ReportsExport
    };

    public static IReadOnlyList<string> AllPermissions => AllPermissionsArray;

    public static IReadOnlyDictionary<string, string> Descriptions { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [WeatherRead] = "Read the shared weather forecast data.",
            [WeatherWrite] = "Create or update weather forecasts.",
            [UsersRead] = "List user records and view details.",
            [UsersWrite] = "Create or update user profiles.",
            [UsersDelete] = "Remove user accounts.",
            [ReportsView] = "View operational reports.",
            [ReportsExport] = "Export reports for download or analysis."
        };
}