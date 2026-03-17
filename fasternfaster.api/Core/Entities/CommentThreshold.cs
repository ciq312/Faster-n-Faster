using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FasterNFaster.Api.Core.Entities;

[Table("comment_thresholds")]
public class CommentThreshold
{
    [Column("id")]
    public Guid Id { get; private set; }

    [Column("min_wpm")]
    public double MinWpm { get; private set; }

    [Column("max_wpm")]
    public double MaxWpm { get; private set; }

    [Column("comment_text")]
    [MaxLength(500)]
    public string CommentText { get; private set; } = null!;

    [Column("cooldown_seconds")]
    public int CooldownSeconds { get; private set; } = 10;

    private CommentThreshold() { } // EF constructor

    public CommentThreshold(
        double minWpm,
        double maxWpm,
        string commentText,
        int cooldownSeconds = 10
    )
    {
        if (minWpm >= maxWpm)
            throw new ArgumentException("MinWpm must be less than MaxWpm.");

        if (string.IsNullOrWhiteSpace(commentText))
            throw new ArgumentException("Comment text is required.");

        if (cooldownSeconds < 0)
            throw new ArgumentOutOfRangeException(
                nameof(cooldownSeconds),
                "Cooldown cannot be negative."
            );

        Id = Guid.NewGuid();
        MinWpm = minWpm;
        MaxWpm = maxWpm;
        CommentText = commentText;
        CooldownSeconds = cooldownSeconds;
    }

    /// <summary>Returns true if the given WPM falls within this threshold's range (inclusive min, exclusive max).</summary>
    public bool Matches(double wpm) => wpm >= MinWpm && wpm < MaxWpm;
}
