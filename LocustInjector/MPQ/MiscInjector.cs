using System.Globalization;
using System.Text.RegularExpressions;

public static class MiscInjector
{
    public const string TargetMiscFileName = "war3mapMisc.txt";
    private const string MaxUnitSpeedKey = "MaxUnitSpeed";
    private const double RequiredMaxUnitSpeed = 522.0;

    /// <summary>
    /// Ensures the given war3mapMisc.txt content contains a MaxUnitSpeed entry of at
    /// least <see cref="RequiredMaxUnitSpeed"/>. Handles three cases:
    /// 1. No content at all (file didn't exist) - creates a new [Misc] section.
    /// 2. Content exists but has no MaxUnitSpeed line - appends it under [Misc].
    /// 3. Content exists with a MaxUnitSpeed line lower than required - raises it.
    /// If the existing value is already >= required, the content is returned unchanged.
    /// </summary>
    public static string InjectMaxUnitSpeed(string? miscContent)
    {
        if (string.IsNullOrEmpty(miscContent))
        {
            return $"[Misc]\r\n{MaxUnitSpeedKey}={RequiredMaxUnitSpeed.ToString("0.0", CultureInfo.InvariantCulture)}\r\n";
        }

        var maxUnitSpeedRegex = new Regex(
            $@"^(\s*{MaxUnitSpeedKey}\s*=\s*)(-?\d+(?:\.\d+)?)\s*$",
            RegexOptions.Multiline);

        var match = maxUnitSpeedRegex.Match(miscContent);
        if (match.Success)
        {
            var currentValue = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            if (currentValue >= RequiredMaxUnitSpeed)
                return miscContent;

            return maxUnitSpeedRegex.Replace(miscContent,
                m => $"{m.Groups[1].Value}{RequiredMaxUnitSpeed.ToString("0.0", CultureInfo.InvariantCulture)}",
                1);
        }

        var miscSectionRegex = new Regex(@"(\[Misc\]\s*\r?\n)", RegexOptions.IgnoreCase);
        var sectionMatch = miscSectionRegex.Match(miscContent);
        var newLine = $"{MaxUnitSpeedKey}={RequiredMaxUnitSpeed.ToString("0.0", CultureInfo.InvariantCulture)}\r\n";

        if (sectionMatch.Success)
        {
            var insertIndex = sectionMatch.Index + sectionMatch.Length;
            return miscContent[..insertIndex] + newLine + miscContent[insertIndex..];
        }

        return $"[Misc]\r\n{newLine}{miscContent}";
    }
}