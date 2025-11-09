namespace AgainstTheSpread.Core.Models;

/// <summary>
/// Represents a single game with betting line information
/// </summary>
public class Game
{
    /// <summary>
    /// The favored team name
    /// </summary>
    public string Favorite { get; set; } = string.Empty;

    /// <summary>
    /// The point spread (negative number indicating favorite margin)
    /// </summary>
    public decimal Line { get; set; }

    /// <summary>
    /// Indicates whether the game is at the favorite's location or neutral (vs/at)
    /// </summary>
    public string VsAt { get; set; } = string.Empty;

    /// <summary>
    /// The underdog team name
    /// </summary>
    public string Underdog { get; set; } = string.Empty;

    /// <summary>
    /// Date and time when the game is scheduled
    /// </summary>
    public DateTime GameDate { get; set; }

    /// <summary>
    /// Display string for the favorite with line (e.g., "Alabama -9.5")
    /// </summary>
    public string FavoriteDisplay => $"{Favorite} {Line}";

    /// <summary>
    /// Display string for the underdog
    /// </summary>
    public string UnderdogDisplay => Underdog;

    /// <summary>
    /// Full game description for display (e.g., "Alabama -9.5 vs Florida State")
    /// </summary>
    public string GameDescription => $"{Favorite} {Line} {VsAt} {Underdog}";
}
