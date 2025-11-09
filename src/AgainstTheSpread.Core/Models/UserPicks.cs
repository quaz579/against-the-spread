namespace AgainstTheSpread.Core.Models;

/// <summary>
/// Represents a user's picks for a given week
/// </summary>
public class UserPicks
{
    /// <summary>
    /// User's name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Week number for these picks
    /// </summary>
    public int Week { get; set; }

    /// <summary>
    /// List of team names selected (must be exactly 6)
    /// </summary>
    public List<string> Picks { get; set; } = new();

    /// <summary>
    /// When the picks were submitted
    /// </summary>
    public DateTime SubmittedAt { get; set; }

    /// <summary>
    /// Year of the season
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Number of picks required (always 6 for MVP)
    /// </summary>
    public const int RequiredPickCount = 6;

    /// <summary>
    /// Validates that the picks data is complete and correct
    /// </summary>
    /// <returns>True if valid (has name, correct week, and exactly 6 picks)</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Name)
               && Week > 0
               && Week <= 14
               && Picks.Count == RequiredPickCount
               && Picks.All(p => !string.IsNullOrWhiteSpace(p))
               && Year >= 2020;
    }

    /// <summary>
    /// Returns a validation error message if picks are invalid
    /// </summary>
    public string? GetValidationError()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return "Name is required";

        if (Week <= 0 || Week > 14)
            return "Week must be between 1 and 14";

        if (Picks.Count != RequiredPickCount)
            return $"Exactly {RequiredPickCount} picks are required (you have {Picks.Count})";

        if (Picks.Any(p => string.IsNullOrWhiteSpace(p)))
            return "All picks must have a team name";

        if (Year < 2020)
            return "Invalid year";

        return null;
    }
}
