using AgainstTheSpread.Functions.Authentication;
using AwesomeAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AgainstTheSpread.Tests.Functions;

public class AdminAuthorizationServiceTests
{
    [Fact]
    public async Task AuthorizeAsync_MissingGoogleToken_ReturnsUnauthorizedAndIgnoresSwaHeaders()
    {
        var validator = Substitute.For<IGoogleIdTokenValidator>();
        var service = CreateService(validator, "admin@example.com");
        var request = CreateRequest(
            null,
            ("Authorization", "Bearer swa-platform-token"),
            ("X-MS-CLIENT-PRINCIPAL", "forged"));

        var result = await service.AuthorizeAsync(request, CancellationToken.None);

        result.Status.Should().Be(AdminAuthorizationStatus.Unauthorized);
        result.Email.Should().BeNull();
        validator.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task AuthorizeAsync_UsesSwaPreservedGoogleTokenHeaderInsteadOfPlatformAuthorization()
    {
        var validator = ValidatorReturning(new GoogleIdentity("admin@example.com", true));
        var service = CreateService(validator, "admin@example.com");
        var request = CreateRequest(
            "valid-token",
            ("Authorization", "Bearer swa-platform-token"));

        var result = await service.AuthorizeAsync(request, CancellationToken.None);

        result.Status.Should().Be(AdminAuthorizationStatus.Authorized);
        result.Email.Should().Be("admin@example.com");
        await validator.Received(1).ValidateAsync("valid-token", Arg.Any<CancellationToken>());
        await validator.DidNotReceive().ValidateAsync("swa-platform-token", Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("token with spaces")]
    public async Task AuthorizeAsync_MalformedGoogleTokenHeader_ReturnsUnauthorized(string header)
    {
        var validator = Substitute.For<IGoogleIdTokenValidator>();
        var service = CreateService(validator, "admin@example.com");

        var result = await service.AuthorizeAsync(CreateRequest(header), CancellationToken.None);

        result.Status.Should().Be(AdminAuthorizationStatus.Unauthorized);
        validator.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task AuthorizeAsync_ValidatorRejectsToken_ReturnsUnauthorized()
    {
        var validator = Substitute.For<IGoogleIdTokenValidator>();
        validator.ValidateAsync("rejected-token", Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("sensitive validation detail"));
        var service = CreateService(validator, "admin@example.com");

        var result = await service.AuthorizeAsync(CreateRequest("rejected-token"), CancellationToken.None);

        result.Status.Should().Be(AdminAuthorizationStatus.Unauthorized);
        result.Email.Should().BeNull();
    }

    [Fact]
    public async Task AuthorizeAsync_UnverifiedEmail_ReturnsUnauthorized()
    {
        var validator = ValidatorReturning(new GoogleIdentity("admin@example.com", false));
        var service = CreateService(validator, "admin@example.com");

        var result = await service.AuthorizeAsync(CreateRequest("valid-token"), CancellationToken.None);

        result.Status.Should().Be(AdminAuthorizationStatus.Unauthorized);
        result.Email.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AuthorizeAsync_EmptyEmail_ReturnsUnauthorized(string? email)
    {
        var validator = ValidatorReturning(new GoogleIdentity(email, true));
        var service = CreateService(validator, "admin@example.com");

        var result = await service.AuthorizeAsync(CreateRequest("valid-token"), CancellationToken.None);

        result.Status.Should().Be(AdminAuthorizationStatus.Unauthorized);
        result.Email.Should().BeNull();
    }

    [Fact]
    public async Task AuthorizeAsync_EmailNotInAllowlist_ReturnsForbidden()
    {
        var validator = ValidatorReturning(new GoogleIdentity("other@example.com", true));
        var service = CreateService(validator, "admin@example.com");

        var result = await service.AuthorizeAsync(CreateRequest("valid-token"), CancellationToken.None);

        result.Status.Should().Be(AdminAuthorizationStatus.Forbidden);
        result.Email.Should().BeNull();
    }

    [Fact]
    public async Task AuthorizeAsync_AllowlistedEmailCaseInsensitively_ReturnsVerifiedEmail()
    {
        var validator = ValidatorReturning(new GoogleIdentity("Admin@Example.COM", true));
        var service = CreateService(validator, " first@example.com, admin@example.com ");

        var result = await service.AuthorizeAsync(CreateRequest("valid-token"), CancellationToken.None);

        result.Status.Should().Be(AdminAuthorizationStatus.Authorized);
        result.Email.Should().Be("Admin@Example.COM");
        await validator.Received(1).ValidateAsync("valid-token", Arg.Any<CancellationToken>());
    }

    private static AdminAuthorizationService CreateService(
        IGoogleIdTokenValidator validator,
        string adminEmails)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ADMIN_EMAILS"] = adminEmails
            })
            .Build();

        return new AdminAuthorizationService(validator, configuration);
    }

    private static IGoogleIdTokenValidator ValidatorReturning(GoogleIdentity identity)
    {
        var validator = Substitute.For<IGoogleIdTokenValidator>();
        validator.ValidateAsync("valid-token", Arg.Any<CancellationToken>())
            .Returns(identity);
        return validator;
    }

    private static HttpRequestData CreateRequest(
        string? googleIdToken,
        params (string Name, string Value)[] additionalHeaders)
    {
        var context = Substitute.For<FunctionContext>();
        var request = Substitute.For<HttpRequestData>(context);
        var headers = new HttpHeadersCollection();

        if (googleIdToken is not null)
        {
            headers.Add("X-Google-ID-Token", googleIdToken);
        }

        foreach (var (name, value) in additionalHeaders)
        {
            headers.Add(name, value);
        }

        request.Headers.Returns(headers);
        return request;
    }
}
