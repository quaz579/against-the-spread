using AgainstTheSpread.Web.Helpers;
using FluentAssertions;

namespace AgainstTheSpread.Tests.Web.Helpers;

public class PrintPageGeneratorTests
{
    [Fact]
    public void GeneratePicksHtml_WithValidPicks_ContainsAllPicks()
    {
        // Arrange
        var picks = new List<string> { "-7 Alabama", "+3 Texas", "-10.5 Georgia" };

        // Act
        var result = PrintPageGenerator.GeneratePicksHtml(picks, 1);

        // Assert
        result.Should().Contain("-7 Alabama");
        result.Should().Contain("+3 Texas");
        result.Should().Contain("-10.5 Georgia");
    }

    [Fact]
    public void GeneratePicksHtml_ContainsCorrectWeekInTitle()
    {
        // Arrange
        var picks = new List<string> { "-7 Alabama" };
        var week = 5;

        // Act
        var result = PrintPageGenerator.GeneratePicksHtml(picks, week);

        // Assert
        result.Should().Contain($"<title>My Picks - Week {week}</title>");
    }

    [Fact]
    public void GeneratePicksHtml_ContainsCloseWindowButton()
    {
        // Arrange
        var picks = new List<string> { "-7 Alabama" };

        // Act
        var result = PrintPageGenerator.GeneratePicksHtml(picks, 1);

        // Assert
        result.Should().Contain("Close Window");
        result.Should().Contain("window.close()");
    }

    [Fact]
    public void GeneratePicksHtml_ContainsBackToPicksLink()
    {
        // Arrange
        var picks = new List<string> { "-7 Alabama" };

        // Act
        var result = PrintPageGenerator.GeneratePicksHtml(picks, 1);

        // Assert
        result.Should().Contain("Back to Picks");
        result.Should().Contain("href=\"/picks\"");
    }

    [Fact]
    public void GeneratePicksHtml_ContainsNavigationControls()
    {
        // Arrange
        var picks = new List<string> { "-7 Alabama" };

        // Act
        var result = PrintPageGenerator.GeneratePicksHtml(picks, 1);

        // Assert
        result.Should().Contain("nav-controls");
        result.Should().Contain("nav-btn");
        result.Should().Contain("nav-btn-primary");
        result.Should().Contain("nav-btn-secondary");
    }

    [Fact]
    public void GeneratePicksHtml_HidesNavigationControlsInPrintMedia()
    {
        // Arrange
        var picks = new List<string> { "-7 Alabama" };

        // Act
        var result = PrintPageGenerator.GeneratePicksHtml(picks, 1);

        // Assert
        result.Should().Contain("@media print");
        result.Should().Contain(".nav-controls");
        result.Should().Contain("display: none");
    }

    [Fact]
    public void GeneratePicksHtml_WithEmptyPicks_GeneratesValidHtml()
    {
        // Arrange
        var picks = new List<string>();

        // Act
        var result = PrintPageGenerator.GeneratePicksHtml(picks, 1);

        // Assert
        result.Should().Contain("<!DOCTYPE html>");
        result.Should().Contain("</html>");
        result.Should().Contain("picks-container");
    }

    [Fact]
    public void GeneratePicksHtml_EscapesHtmlCharactersInPicks()
    {
        // Arrange
        var picks = new List<string> { "<script>alert('xss')</script>", "Team & Other" };

        // Act
        var result = PrintPageGenerator.GeneratePicksHtml(picks, 1);

        // Assert
        result.Should().NotContain("<script>alert('xss')</script>");
        result.Should().Contain("&lt;script&gt;");
        result.Should().Contain("&amp;");
    }

    [Fact]
    public void GeneratePicksHtml_ThrowsArgumentNullException_WhenPicksIsNull()
    {
        // Act & Assert
        var act = () => PrintPageGenerator.GeneratePicksHtml(null!, 1);
        act.Should().Throw<ArgumentNullException>().WithParameterName("picksLines");
    }

    [Fact]
    public void GeneratePicksHtml_ContainsValidHtmlStructure()
    {
        // Arrange
        var picks = new List<string> { "-7 Alabama" };

        // Act
        var result = PrintPageGenerator.GeneratePicksHtml(picks, 1);

        // Assert
        result.Should().Contain("<!DOCTYPE html>");
        result.Should().Contain("<html lang=\"en\">");
        result.Should().Contain("<head>");
        result.Should().Contain("</head>");
        result.Should().Contain("<body>");
        result.Should().Contain("</body>");
        result.Should().Contain("</html>");
    }

    [Fact]
    public void GeneratePicksHtml_ContainsViewportMeta()
    {
        // Arrange
        var picks = new List<string> { "-7 Alabama" };

        // Act
        var result = PrintPageGenerator.GeneratePicksHtml(picks, 1);

        // Assert
        result.Should().Contain("viewport");
        result.Should().Contain("width=device-width");
    }

    [Fact]
    public void GeneratePicksHtml_WrapsEachPickInDiv()
    {
        // Arrange
        var picks = new List<string> { "-7 Alabama", "+3 Texas" };

        // Act
        var result = PrintPageGenerator.GeneratePicksHtml(picks, 1);

        // Assert
        result.Should().Contain("<div>-7 Alabama</div>");
        result.Should().Contain("<div>+3 Texas</div>");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(12)]
    [InlineData(15)]
    public void GeneratePicksHtml_HandlesVariousWeekNumbers(int week)
    {
        // Arrange
        var picks = new List<string> { "-7 Alabama" };

        // Act
        var result = PrintPageGenerator.GeneratePicksHtml(picks, week);

        // Assert
        result.Should().Contain($"Week {week}");
    }

    [Fact]
    public void GeneratePicksHtml_ContainsSolidBackgroundForAccessibility()
    {
        // Arrange
        var picks = new List<string> { "-7 Alabama" };

        // Act
        var result = PrintPageGenerator.GeneratePicksHtml(picks, 1);

        // Assert
        result.Should().Contain("background-color: #ffffff");
        result.Should().Contain("border-top: 1px solid #dee2e6");
    }
}
