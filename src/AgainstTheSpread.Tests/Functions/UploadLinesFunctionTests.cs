using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Functions;
using AgainstTheSpread.Functions.Authentication;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AgainstTheSpread.Tests.Functions;

public class UploadLinesFunctionTests
{
    [Fact]
    public void Constructor_WithSharedAuthorizationService_CreatesInstance()
    {
        var function = new UploadLinesFunction(
            Substitute.For<ILogger<UploadLinesFunction>>(),
            Substitute.For<IExcelService>(),
            Substitute.For<IStorageService>(),
            Substitute.For<IAdminAuthorizationService>());

        function.Should().NotBeNull();
    }
}
