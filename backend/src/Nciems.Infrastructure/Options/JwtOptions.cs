namespace Nciems.Infrastructure.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "Nciems";
    public string Audience { get; set; } = "Nciems.Client";
    public string Key { get; set; } = "REPLACE_WITH_32_BYTE_MINIMUM_SECRET_KEY";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}
