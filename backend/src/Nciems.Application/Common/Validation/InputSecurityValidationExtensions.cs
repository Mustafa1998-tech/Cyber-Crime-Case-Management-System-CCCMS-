using System.Text.RegularExpressions;
using FluentValidation;

namespace Nciems.Application.Common.Validation;

public static partial class InputSecurityValidationExtensions
{
    private static readonly Regex[] XssPatterns =
    [
        ScriptTagRegex(),
        JavascriptProtocolRegex(),
        InlineEventHandlerRegex(),
        HtmlRiskTagRegex(),
        HtmlDataUriRegex()
    ];

    private static readonly string[] SqlInjectionMarkers =
    [
        " union select ",
        " drop table ",
        " delete from ",
        " insert into ",
        " update ",
        " exec ",
        " xp_",
        " information_schema ",
        " waitfor delay ",
        " or 1=1",
        " and 1=1",
        " ;--",
        "'--"
    ];

    public static IRuleBuilderOptions<T, string> MustBeSafeText<T>(
        this IRuleBuilder<T, string> ruleBuilder,
        string fieldName,
        bool checkSqlPatterns = true)
    {
        return ruleBuilder
            .Must(value => !ContainsThreatPatterns(value, checkSqlPatterns))
            .WithMessage($"{fieldName} contains unsafe content.");
    }

    public static IRuleBuilderOptions<T, string?> MustBeSafeOptionalText<T>(
        this IRuleBuilder<T, string?> ruleBuilder,
        string fieldName,
        bool checkSqlPatterns = true)
    {
        return ruleBuilder
            .Must(value => string.IsNullOrWhiteSpace(value) || !ContainsThreatPatterns(value, checkSqlPatterns))
            .WithMessage($"{fieldName} contains unsafe content.");
    }

    internal static bool ContainsThreatPatterns(string value, bool checkSqlPatterns)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (value.Contains('\0'))
        {
            return true;
        }

        if (XssPatterns.Any(pattern => pattern.IsMatch(value)))
        {
            return true;
        }

        if (!checkSqlPatterns)
        {
            return false;
        }

        var normalized = $" {value.ToLowerInvariant()} ";
        return SqlInjectionMarkers.Any(marker => normalized.Contains(marker, StringComparison.Ordinal));
    }

    [GeneratedRegex(@"<\s*script\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ScriptTagRegex();

    [GeneratedRegex(@"javascript\s*:", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex JavascriptProtocolRegex();

    [GeneratedRegex(@"\bon\w+\s*=", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex InlineEventHandlerRegex();

    [GeneratedRegex(@"<\s*(iframe|object|embed|svg)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HtmlRiskTagRegex();

    [GeneratedRegex(@"data\s*:\s*text\/html", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HtmlDataUriRegex();
}
