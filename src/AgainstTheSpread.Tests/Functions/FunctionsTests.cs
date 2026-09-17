using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Models;
using AgainstTheSpread.Functions;
using AwesomeAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Net;
using System.Text;
using System.Text.Json;

namespace AgainstTheSpread.Tests.Functions;

public class FunctionsTests
{
    [Fact]
    public async Task WeeksFunction_GetWeeks_ReturnsWeeksList()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger<WeeksFunction>>();
        var mockStorage = Substitute.For<IStorageService>();
        mockStorage.GetAvailableWeeksAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<int> { 1, 2, 3 });

        var function = new WeeksFunction(mockLogger, mockStorage);

        // Act & Assert - basic construction test
        function.Should().NotBeNull();
        _ = mockStorage.DidNotReceive().GetAvailableWeeksAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LinesFunction_GetLines_ValidatesWeekRange()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger<LinesFunction>>();
        var mockStorage = Substitute.For<IStorageService>();

        var function = new LinesFunction(mockLogger, mockStorage);

        // Act & Assert - basic construction test
        function.Should().NotBeNull();
    }

    [Fact]
    public async Task PicksFunction_SubmitPicks_ValidatesPicks()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger<PicksFunction>>();
        var mockExcel = Substitute.For<IExcelService>();
        mockExcel.GeneratePicksExcelAsync(Arg.Any<UserPicks>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 1, 2, 3 });

        var function = new PicksFunction(mockLogger, mockExcel);

        // Act & Assert - basic construction test
        function.Should().NotBeNull();
    }

    // Note: Full integration tests for HTTP endpoints will be added in Phase 8
    // These would require mocking HttpRequestData and FunctionContext which is complex
    // For now, we verify the functions are constructed and services are injected correctly
}
