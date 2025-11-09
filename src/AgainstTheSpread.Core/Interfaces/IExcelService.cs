using AgainstTheSpread.Core.Models;

namespace AgainstTheSpread.Core.Interfaces;

/// <summary>
/// Service for parsing and generating Excel files
/// </summary>
public interface IExcelService
{
    /// <summary>
    /// Parses an Excel file containing weekly betting lines
    /// </summary>
    /// <param name="excelStream">Stream containing the Excel file data</param>
    /// <returns>WeeklyLines object with parsed game data</returns>
    WeeklyLines ParseLinesFromExcel(Stream excelStream);

    /// <summary>
    /// Generates an Excel file with user's picks in the expected format
    /// </summary>
    /// <param name="picks">User's picks to include in the Excel file</param>
    /// <returns>Stream containing the generated Excel file</returns>
    Stream GeneratePicksExcel(UserPicks picks);
}
