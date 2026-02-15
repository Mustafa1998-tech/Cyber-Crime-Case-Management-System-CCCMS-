namespace Nciems.Infrastructure.Options;

public sealed class BootstrapAdminOptions
{
    public const string SectionName = "BootstrapAdmin";

    public string UserName { get; set; } = "superadmin";
    public string Email { get; set; } = "admin@nciems.local";
    public string Password { get; set; } = "ChangeThisImmediately!123";
}
