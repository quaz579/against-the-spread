using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Models;
using AgainstTheSpread.Functions;
using AgainstTheSpread.Functions.Authentication;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Collections.Specialized;
using System.Net;
using System.Text.Json;
using Azure.Core.Serialization;

namespace AgainstTheSpread.Tests.Functions;

public class UploadFunctionAuthorizationTests
{
    [Fact]
    public async Task UploadLines_Run_InvokesSharedAuthorizationBeforeProcessing()
    {
        var authorization = DenyingAuthorization();
        var excel = new Mock<IExcelService>(MockBehavior.Strict);
        var storage = new Mock<IStorageService>(MockBehavior.Strict);
        var function = new UploadLinesFunction(
            Mock.Of<ILogger<UploadLinesFunction>>(),
            excel.Object,
            storage.Object,
            authorization.Object);
        var request = CreateRequest();

        var response = await function.Run(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        authorization.Verify(
            a => a.AuthorizeAsync(request, It.IsAny<CancellationToken>()),
            Times.Once);
        excel.VerifyNoOtherCalls();
        storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UploadBowlLines_Run_InvokesSharedAuthorizationBeforeProcessing()
    {
        var authorization = DenyingAuthorization();
        var excel = new Mock<IBowlExcelService>(MockBehavior.Strict);
        var storage = new Mock<IStorageService>(MockBehavior.Strict);
        var function = new UploadBowlLinesFunction(
            Mock.Of<ILogger<UploadBowlLinesFunction>>(),
            excel.Object,
            storage.Object,
            authorization.Object);
        var request = CreateRequest();

        var response = await function.Run(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        authorization.Verify(
            a => a.AuthorizeAsync(request, It.IsAny<CancellationToken>()),
            Times.Once);
        excel.VerifyNoOtherCalls();
        storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UploadLines_Run_WhenAuthorized_ParsesAndStoresRawWorkbookBody()
    {
        var workbook = new byte[] { 0x50, 0x4b, 0x03, 0x04 };
        var weeklyLines = new WeeklyLines
        {
            Week = 1,
            Year = 2026,
            Games = new List<Game> { new() { Favorite = "A", Underdog = "B" } }
        };
        var authorization = AuthorizedAuthorization();
        var excel = new Mock<IExcelService>(MockBehavior.Strict);
        excel.Setup(e => e.ParseWeeklyLinesAsync(
                It.Is<Stream>(s => StreamMatches(s, workbook)),
                1,
                2026,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(weeklyLines);
        var storage = new Mock<IStorageService>(MockBehavior.Strict);
        storage.Setup(s => s.UploadWeeklyLinesAsync(
                It.Is<Stream>(stream => StreamMatches(stream, workbook)),
                1,
                2026,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("stored");
        var function = new UploadLinesFunction(
            Mock.Of<ILogger<UploadLinesFunction>>(),
            excel.Object,
            storage.Object,
            authorization.Object);
        var request = CreateRequest(
            new NameValueCollection { ["week"] = "1", ["year"] = "2026" },
            workbook);

        var response = await function.Run(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        authorization.Verify(a => a.AuthorizeAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        excel.VerifyAll();
        storage.VerifyAll();
    }

    [Fact]
    public async Task UploadBowlLines_Run_WhenAuthorized_ParsesAndStoresRawWorkbookBody()
    {
        var workbook = new byte[] { 0x50, 0x4b, 0x03, 0x04 };
        var bowlLines = new BowlLines
        {
            Year = 2026,
            Games = new List<BowlGame> { new() { GameNumber = 1, Favorite = "A", Underdog = "B" } }
        };
        var authorization = AuthorizedAuthorization();
        var excel = new Mock<IBowlExcelService>(MockBehavior.Strict);
        excel.Setup(e => e.ParseBowlLinesAsync(
                It.Is<Stream>(s => StreamMatches(s, workbook)),
                2026,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(bowlLines);
        var storage = new Mock<IStorageService>(MockBehavior.Strict);
        storage.Setup(s => s.UploadBowlLinesAsync(
                It.Is<Stream>(stream => StreamMatches(stream, workbook)),
                2026,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("stored");
        var function = new UploadBowlLinesFunction(
            Mock.Of<ILogger<UploadBowlLinesFunction>>(),
            excel.Object,
            storage.Object,
            authorization.Object);
        var request = CreateRequest(
            new NameValueCollection { ["year"] = "2026" },
            workbook);

        var response = await function.Run(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        authorization.Verify(a => a.AuthorizeAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        excel.VerifyAll();
        storage.VerifyAll();
    }

    private static Mock<IAdminAuthorizationService> DenyingAuthorization()
    {
        var authorization = new Mock<IAdminAuthorizationService>();
        authorization
            .Setup(a => a.AuthorizeAsync(It.IsAny<HttpRequestData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdminAuthorizationResult(AdminAuthorizationStatus.Unauthorized));
        return authorization;
    }

    private static Mock<IAdminAuthorizationService> AuthorizedAuthorization()
    {
        var authorization = new Mock<IAdminAuthorizationService>();
        authorization
            .Setup(a => a.AuthorizeAsync(It.IsAny<HttpRequestData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdminAuthorizationResult(AdminAuthorizationStatus.Authorized, "admin@example.test"));
        return authorization;
    }

    private static HttpRequestData CreateRequest(
        NameValueCollection? query = null,
        byte[]? body = null)
    {
        var context = new Mock<FunctionContext>();
        var workerOptions = Options.Create(new WorkerOptions
        {
            Serializer = new JsonObjectSerializer(new JsonSerializerOptions(JsonSerializerDefaults.Web))
        });
        var services = new ServiceCollection()
            .AddSingleton<IOptions<WorkerOptions>>(workerOptions)
            .BuildServiceProvider();
        context.SetupGet(c => c.InstanceServices).Returns(services);
        var response = new Mock<HttpResponseData>(context.Object);
        response.SetupProperty(r => r.StatusCode);
        response.SetupGet(r => r.Headers).Returns(new HttpHeadersCollection());
        response.SetupProperty(r => r.Body, new MemoryStream());

        var request = new Mock<HttpRequestData>(context.Object);
        request.SetupGet(r => r.Query).Returns(query ?? new NameValueCollection());
        request.SetupGet(r => r.Body).Returns(new MemoryStream(body ?? Array.Empty<byte>()));
        request.Setup(r => r.CreateResponse()).Returns(response.Object);
        return request.Object;
    }

    private static bool StreamMatches(Stream stream, byte[] expected)
    {
        var originalPosition = stream.Position;
        stream.Position = 0;
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        stream.Position = originalPosition;
        return copy.ToArray().SequenceEqual(expected);
    }
}
