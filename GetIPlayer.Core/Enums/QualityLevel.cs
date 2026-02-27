namespace GetIPlayer.Core.Enums;

/// <summary>
/// Quality level for stream selection.
/// </summary>
public enum QualityLevel
{
    /// <summary>High definition (1280x720 or higher).</summary>
    Hd = 4,

    /// <summary>Standard definition (704x396 @ 50fps typically).</summary>
    Sd = 3,

    /// <summary>Web quality (lower bitrate).</summary>
    Web = 2,

    /// <summary>Mobile quality (lowest bitrate).</summary>
    Mobile = 1
}
