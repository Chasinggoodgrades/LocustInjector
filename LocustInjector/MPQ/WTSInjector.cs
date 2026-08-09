using System.Text.RegularExpressions;

public static class WTSInjector
{
    public const string TargetWtsFileName = "war3map.wts";
    private const string Suffix = "_vAches";

    /// <summary>
    /// Appends the "_vAches" suffix to the map-name text stored in STRING 3 of the
    /// given war3map.wts content, if that block exists. Returns the content
    /// unchanged if STRING 3 is not found.
    /// </summary>
    public static string InjectMapNameSuffix(string wtsContent, string stringValue = "3")
    {
        var regex = new Regex(
            $@"(STRING\s+{Regex.Escape(stringValue)}\s*\r?\n\{{\r?\n)(.*?)(\r?\n\}})",
            RegexOptions.Singleline | RegexOptions.Compiled);

        return regex.Replace(wtsContent, match =>
        {
            var header = match.Groups[1].Value;
            var body = match.Groups[2].Value;
            var footer = match.Groups[3].Value;

            return header + AppendSuffix(body) + footer;
        }, 1);
    }

    /// <summary>
    /// Inserts the suffix after the visible name text: right before a trailing
    /// "|r" color-reset token if present (e.g. "|cff3579dc{NAME}  |r" ->
    /// "|cff3579dc{NAME}_vAches  |r"), otherwise at the very end of the content
    /// (e.g. "|cff...SomeNameHere |cff...Extra" -> "...Extra_vAches").
    /// </summary>
    private static string AppendSuffix(string body)
    {
        var colorResetIndex = body.LastIndexOf("|r", StringComparison.Ordinal);
        if (colorResetIndex >= 0)
        {
            var before = body[..colorResetIndex].TrimEnd();
            var after = body[colorResetIndex..];
            return $"{before}{Suffix} {after}";
        }

        return body.TrimEnd() + Suffix;
    }
}
