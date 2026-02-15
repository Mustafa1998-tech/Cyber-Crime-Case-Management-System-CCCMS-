using Nciems.Application.Features.Auth;
using Nciems.Application.Features.Complaints;
using Nciems.Application.Features.Search;

namespace Nciems.Security.Tests;

public sealed class InputValidationSecurityTests
{
    [Fact]
    public void CreateComplaintValidator_ShouldAccept_ValidInput()
    {
        var validator = new CreateComplaintCommandValidator();
        var command = new CreateComplaintCommand(
            "Ahmed Hassan",
            "+249912345678",
            "Fraud",
            "Fraudulent transfer reported from mobile wallet.");

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateComplaintValidator_ShouldReject_XssPayload()
    {
        var validator = new CreateComplaintCommandValidator();
        var command = new CreateComplaintCommand(
            "Ahmed Hassan",
            "+249912345678",
            "Fraud",
            "<script>alert('xss')</script>");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateComplaintCommand.Description));
    }

    [Fact]
    public void CreateComplaintValidator_ShouldReject_SqlInjectionPayload()
    {
        var validator = new CreateComplaintCommandValidator();
        var command = new CreateComplaintCommand(
            "Ahmed' OR 1=1 --",
            "+249912345678",
            "Fraud",
            "Attempted account abuse.");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateComplaintCommand.ComplainantName));
    }

    [Fact]
    public void GlobalSearchQueryValidator_ShouldReject_InvalidHash()
    {
        var validator = new GlobalSearchQueryValidator();
        var query = new GlobalSearchQuery(null, "hash-not-valid", null, null, null);

        var result = validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(GlobalSearchQuery.Hash));
    }

    [Fact]
    public void GlobalSearchQueryValidator_ShouldReject_SuspectNameInjection()
    {
        var validator = new GlobalSearchQueryValidator();
        var query = new GlobalSearchQuery(null, null, null, null, "union select password from Users");

        var result = validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(GlobalSearchQuery.SuspectName));
    }

    [Fact]
    public void RegisterUserValidator_ShouldEnforce_StrongPassword()
    {
        var validator = new RegisterUserCommandValidator();
        var weakCommand = new RegisterUserCommand(
            "investigator.two",
            "investigator.two@govportal.com",
            "simplepassword",
            true,
            ["Investigator"]);

        var weakResult = validator.Validate(weakCommand);

        Assert.False(weakResult.IsValid);
        Assert.Contains(weakResult.Errors, error => error.PropertyName == nameof(RegisterUserCommand.Password));
    }
}
