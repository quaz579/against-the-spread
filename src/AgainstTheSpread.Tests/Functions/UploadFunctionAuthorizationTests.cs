using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Models;
using AgainstTheSpread.Functions;
using AgainstTheSpread.Functions.Authentication;
using AwesomeAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
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
        var excel = Substitute.For<IExcelService>();
        var storage = Substitute.For<IStorageService>();
        var function = new UploadLinesFunction(
            Substitute.For<ILogger<UploadLinesFunction>>(),
            excel,
            storage,
            authorization);
        var request = CreateRequest();

        var response = await function.Run(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await authorization.Received(1).AuthorizeAsync(request, Arg.Any<CancellationToken>());
        excel.ReceivedCalls().Should().BeEmpty();
        storage.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task UploadBowlLines_Run_InvokesSharedAuthorizationBeforeProcessing()
    {
        var authorization = DenyingAuthorization();
        var excel = Substitute.For<IBowlExcelService>();
        var storage = Substitute.For<IStorageService>();
        var function = new UploadBowlLinesFunction(
            Substitute.For<ILogger<UploadBowlLinesFunction>>(),
            excel,
            storage,
            authorization);
        var request = CreateRequest();

        var response = await function.Run(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await authorization.Received(1).AuthorizeAsync(request, Arg.Any<CancellationToken>());
        excel.ReceivedCalls().Should().BeEmpty();
        storage.ReceivedCalls().Should().BeEmpty();
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
        byte[]? excelReceivedBytes = null;
        byte[]? storageReceivedBytes = null;
        var excel = Substitute.For<IExcelService>();
        excel.ParseWeeklyLinesAsync(Arg.Any<Stream>(), 1, 2026, Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                excelReceivedBytes = SnapshotBytes(callInfo.ArgAt<Stream>(0));
                return weeklyLines;
            });
        var storage = Substitute.For<IStorageService>();
        storage.UploadWeeklyLinesAsync(Arg.Any<Stream>(), 1, 2026, Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                storageReceivedBytes = SnapshotBytes(callInfo.ArgAt<Stream>(0));
                return "stored";
            });
        var function = new UploadLinesFunction(
            Substitute.For<ILogger<UploadLinesFunction>>(),
            excel,
            storage,
            authorization);
        var request = CreateRequest(
            new NameValueCollection { ["week"] = "1", ["year"] = "2026" },
            workbook);

        var response = await function.Run(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await authorization.Received(1).AuthorizeAsync(request, Arg.Any<CancellationToken>());
        await excel.Received(1).ParseWeeklyLinesAsync(Arg.Any<Stream>(), 1, 2026, Arg.Any<CancellationToken>());
        excelReceivedBytes.Should().Equal(workbook);
        excel.ReceivedCalls().Should().HaveCount(1);
        await storage.Received(1).UploadWeeklyLinesAsync(Arg.Any<Stream>(), 1, 2026, Arg.Any<CancellationToken>());
        storageReceivedBytes.Should().Equal(workbook);
        storage.ReceivedCalls().Should().HaveCount(1);
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
        byte[]? excelReceivedBytes = null;
        byte[]? storageReceivedBytes = null;
        var excel = Substitute.For<IBowlExcelService>();
        excel.ParseBowlLinesAsync(Arg.Any<Stream>(), 2026, Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                excelReceivedBytes = SnapshotBytes(callInfo.ArgAt<Stream>(0));
                return bowlLines;
            });
        var storage = Substitute.For<IStorageService>();
        storage.UploadBowlLinesAsync(Arg.Any<Stream>(), 2026, Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                storageReceivedBytes = SnapshotBytes(callInfo.ArgAt<Stream>(0));
                return "stored";
            });
        var function = new UploadBowlLinesFunction(
            Substitute.For<ILogger<UploadBowlLinesFunction>>(),
            excel,
            storage,
            authorization);
        var request = CreateRequest(
            new NameValueCollection { ["year"] = "2026" },
            workbook);

        var response = await function.Run(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await authorization.Received(1).AuthorizeAsync(request, Arg.Any<CancellationToken>());
        await excel.Received(1).ParseBowlLinesAsync(Arg.Any<Stream>(), 2026, Arg.Any<CancellationToken>());
        excelReceivedBytes.Should().Equal(workbook);
        excel.ReceivedCalls().Should().HaveCount(1);
        await storage.Received(1).UploadBowlLinesAsync(Arg.Any<Stream>(), 2026, Arg.Any<CancellationToken>());
        storageReceivedBytes.Should().Equal(workbook);
        storage.ReceivedCalls().Should().HaveCount(1);
    }

    [Fact]
    public async Task UploadLines_Run_InvalidWorkbook_ReturnsBadRequestWithoutCallingStorage()
    {
        var excel = Substitute.For<IExcelService>();
        const string message = "Invalid date header at C11: 'Friday, September 19, 2026'. Check the weekday.";
        excel.ParseWeeklyLinesAsync(Arg.Any<Stream>(), 3, 2026, Arg.Any<CancellationToken>())
            .ThrowsAsync(new FormatException(message));
        var storage = Substitute.For<IStorageService>();
        var function = new UploadLinesFunction(Substitute.For<ILogger<UploadLinesFunction>>(),
            excel, storage, AuthorizedAuthorization());
        var request = CreateRequest(new NameValueCollection { ["week"] = "3", ["year"] = "2026" }, new byte[] { 1 });

        var response = await function.Run(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Body.Position = 0;
        using var body = await JsonDocument.ParseAsync(response.Body);
        body.RootElement.GetProperty("error").GetString().Should().Be(message);
        storage.ReceivedCalls().Should().BeEmpty();
    }

    private static IAdminAuthorizationService DenyingAuthorization()
    {
        var authorization = Substitute.For<IAdminAuthorizationService>();
        authorization
            .AuthorizeAsync(Arg.Any<HttpRequestData>(), Arg.Any<CancellationToken>())
            .Returns(new AdminAuthorizationResult(AdminAuthorizationStatus.Unauthorized));
        return authorization;
    }

    private static IAdminAuthorizationService AuthorizedAuthorization()
    {
        var authorization = Substitute.For<IAdminAuthorizationService>();
        authorization
            .AuthorizeAsync(Arg.Any<HttpRequestData>(), Arg.Any<CancellationToken>())
            .Returns(new AdminAuthorizationResult(AdminAuthorizationStatus.Authorized, "admin@example.test"));
        return authorization;
    }

    private static HttpRequestData CreateRequest(
        NameValueCollection? query = null,
        byte[]? body = null)
    {
        var context = Substitute.For<FunctionContext>();
        var workerOptions = Options.Create(new WorkerOptions
        {
            Serializer = new JsonObjectSerializer(new JsonSerializerOptions(JsonSerializerDefaults.Web))
        });
        var services = new ServiceCollection()
            .AddSingleton<IOptions<WorkerOptions>>(workerOptions)
            .BuildServiceProvider();
        context.InstanceServices.Returns(services);
        var response = Substitute.For<HttpResponseData>(context);
        response.Headers.Returns(new HttpHeadersCollection());
        response.Body = new MemoryStream();

        var request = Substitute.For<HttpRequestData>(context);
        request.Query.Returns(query ?? new NameValueCollection());
        request.Body.Returns(new MemoryStream(body ?? Array.Empty<byte>()));
        request.CreateResponse().Returns(response);
        return request;
    }

    // Snapshotting here (inside the substitute's Returns callback, while the SUT's `using`-scoped
    // stream is still open) avoids re-reading the stream during a later Received() assertion, when
    // UploadLinesFunction.Run's `using var stream` has already disposed it.
    private static byte[] SnapshotBytes(Stream stream)
    {
        var originalPosition = stream.Position;
        stream.Position = 0;
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        stream.Position = originalPosition;
        return copy.ToArray();
    }
}
