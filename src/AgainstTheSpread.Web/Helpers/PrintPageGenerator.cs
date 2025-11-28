using System.Net;

namespace AgainstTheSpread.Web.Helpers;

/// <summary>
/// Helper class for generating printable HTML pages for picks.
/// </summary>
public static class PrintPageGenerator
{
    /// <summary>
    /// Generates an HTML page displaying picks with navigation controls.
    /// </summary>
    /// <param name="picksLines">List of formatted pick strings (e.g., "-7 Alabama", "+3 Texas")</param>
    /// <param name="week">The week number for the title</param>
    /// <returns>Complete HTML document as a string</returns>
    public static string GeneratePicksHtml(IEnumerable<string> picksLines, int week)
    {
        if (picksLines == null)
        {
            throw new ArgumentNullException(nameof(picksLines));
        }

        var picksHtmlContent = string.Join("", picksLines.Select(p => $"<div>{WebUtility.HtmlEncode(p)}</div>"));

        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>My Picks - Week {week}</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background-color: #ffffff;
            display: flex;
            flex-direction: column;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
            padding: 20px;
        }}
        .picks-container {{
            text-align: center;
            width: 100%;
        }}
        .picks-container div {{
            font-size: clamp(28px, 8vw, 48px);
            font-weight: bold;
            line-height: 1.6;
            padding: 8px 0;
        }}
        .nav-controls {{
            position: fixed;
            bottom: 0;
            left: 0;
            right: 0;
            padding: 16px;
            background-color: #ffffff;
            border-top: 1px solid #dee2e6;
            display: flex;
            gap: 12px;
            justify-content: center;
        }}
        .nav-btn {{
            padding: 14px 28px;
            font-size: 18px;
            font-weight: bold;
            border: none;
            border-radius: 8px;
            cursor: pointer;
            text-decoration: none;
            display: inline-block;
        }}
        .nav-btn-primary {{
            background-color: #0d6efd;
            color: white;
        }}
        .nav-btn-secondary {{
            background-color: #6c757d;
            color: white;
        }}
        @media print {{
            body {{
                padding: 40px;
            }}
            .picks-container div {{
                font-size: 36pt;
                line-height: 1.8;
            }}
            .nav-controls {{
                display: none;
            }}
        }}
    </style>
</head>
<body>
    <div class=""picks-container"">
        {picksHtmlContent}
    </div>
    <div class=""nav-controls"">
        <button class=""nav-btn nav-btn-primary"" onclick=""window.close(); return false;"">✕ Close Window</button>
        <a class=""nav-btn nav-btn-secondary"" href=""/picks"">← Back to Picks</a>
    </div>
</body>
</html>";
    }
}
