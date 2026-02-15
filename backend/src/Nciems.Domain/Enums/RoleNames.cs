namespace Nciems.Domain.Enums;

public static class RoleNames
{
    public const string SuperAdmin = "SuperAdmin";
    public const string SystemAdmin = "SystemAdmin";
    public const string IntakeOfficer = "IntakeOfficer";
    public const string Investigator = "Investigator";
    public const string ForensicAnalyst = "ForensicAnalyst";
    public const string Prosecutor = "Prosecutor";

    public static readonly string[] PrivilegedRoles =
    [
        SuperAdmin,
        SystemAdmin,
        Investigator,
        ForensicAnalyst
    ];

    public static readonly string[] All =
    [
        SuperAdmin,
        SystemAdmin,
        IntakeOfficer,
        Investigator,
        ForensicAnalyst,
        Prosecutor
    ];
}
