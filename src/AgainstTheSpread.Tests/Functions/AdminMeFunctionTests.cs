using AgainstTheSpread.Functions;
using AgainstTheSpread.Functions.Authentication;
using AwesomeAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Net;
using System.Text.Json;

namespace AgainstTheSpread.Tests.Functions;

public class AdminMeFunctionTests
{
    [Fact]
    public async Task Run_AuthorizedIdentity_ReturnsOnlyVerifiedEmail()
    {
        var authorization = Substitute.For<IAdminAuthorizationService>();
        authorization
            .AuthorizeAsync(Arg.Any<HttpRequestData>(), Arg.Any<CancellationToken>())
            .Returns(new AdminAuthorizationResult(
                AdminAuthorizationStatus.Authorized,
                "verified@example.com"));
        var function = new AdminMeFunction(
            Substitute.For<ILogger<AdminMeFunction>>(),
            authorization);
        var (request, response) = CreateRequest();

        var result = await function.Run(request, CancellationToken.None);

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("Cache-Control").Should().ContainSingle("no-store");
        response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(response.Body);
        document.RootElement.EnumerateObject().Select(p => p.Name).Should().Equal("email");
        document.RootElement.GetProperty("email").GetString().Should().Be("verified@example.com");
    }

    [Theory]
    [InlineData(AdminAuthorizationStatus.Unauthorized, HttpStatusCode.Unauthorized)]
    [InlineData(AdminAuthorizationStatus.Forbidden, HttpStatusCode.Forbidden)]
    public async Task Run_DeniedIdentity_ReturnsGenericError(
        AdminAuthorizationStatus authorizationStatus,
        HttpStatusCode expectedStatus)
    {
        var authorization = Substitute.For<IAdminAuthorizationService>();
        authorization
            .AuthorizeAsync(Arg.Any<HttpRequestData>(), Arg.Any<CancellationToken>())
            .Returns(new AdminAuthorizationResult(authorizationStatus));
        var function = new AdminMeFunction(
            Substitute.For<ILogger<AdminMeFunction>>(),
            authorization);
        var (request, response) = CreateRequest();

        var result = await function.Run(request, CancellationToken.None);

        result.StatusCode.Should().Be(expectedStatus);
        response.Headers.GetValues("Cache-Control").Should().ContainSingle("no-store");
        response.Body.Position = 0;
        using var reader = new StreamReader(response.Body);
        (await reader.ReadToEndAsync()).Should().NotContain("email");
    }

    private static (HttpRequestData Request, HttpResponseData Response) CreateRequest()
    {
        var context = Substitute.For<FunctionContext>();
        var response = Substitute.For<HttpResponseData>(context);
        response.Headers.Returns(new HttpHeadersCollection());
        response.Body = new MemoryStream();

        var request = Substitute.For<HttpRequestData>(context);
        request.CreateResponse().Returns(response);

        return (request, response);
    }
}
