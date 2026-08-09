using System.Text;

/// <summary>
/// An in-memory war3map.j script being built up by a series of injectors.
/// Centralizes the file I/O plus the three insertion points every injector
/// so far has needed: globals, function/trigger definitions, and calls
/// inside main()'s body.
///
/// WC3 JASS files are Windows-1252 (ANSI). Latin1 maps bytes 0-255 directly
/// to the same Unicode code points, so it's a lossless round-trip for any
/// ANSI-encoded file — reading/writing as UTF-8 would corrupt any JASS
/// script containing characters above byte 127.
/// </summary>
public sealed class JassScript
{
    private readonly StringBuilder _sb;

    public string FilePath { get; }
    public int Length => _sb.Length;

    private JassScript(string filePath, string content)
    {
        FilePath = filePath;
        _sb = new StringBuilder(content);
    }

    public static JassScript Load(string outputPath)
    {
        var filePath = Path.Combine(outputPath, "war3map.j");

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"war3map.j not found in {outputPath}", filePath);
        }

        var content = File.ReadAllText(filePath, Encoding.Latin1);
        return new JassScript(filePath, content);
    }

    /// <summary>Build a script from a raw string instead of a file — handy for unit tests.</summary>
    public static JassScript FromString(string content, string filePath = "war3map.j")
        => new(filePath, content);

    public void Save()
    {
        File.WriteAllText(FilePath, _sb.ToString(), Encoding.Latin1);
    }

    public bool Contains(string token) => _sb.ToString().Contains(token);

    public override string ToString() => _sb.ToString();

    /// <summary>
    /// Insert code right before the 'endglobals' keyword. Use this for global
    /// variable declarations (triggers, counters, hashtables, etc).
    /// </summary>
    public void InsertBeforeEndGlobals(string code)
    {
        var index = _sb.ToString().IndexOf("endglobals", StringComparison.Ordinal);

        if (index == -1)
        {
            throw new InvalidOperationException("Could not find 'endglobals' in JASS script");
        }

        _sb.Insert(index, code + "\n");
    }

    /// <summary>
    /// Insert code right before 'function main'. Use this for new function
    /// and trigger definitions. JASS parses the whole script before running
    /// main(), so exactly where a function is defined textually doesn't
    /// matter — this is just a convenient, consistent landing spot.
    /// </summary>
    public void InsertBeforeMainFunction(string code)
    {
        var index = _sb.ToString().IndexOf("function main", StringComparison.Ordinal);

        if (index == -1)
        {
            throw new InvalidOperationException("Could not find 'function main' in JASS script");
        }

        _sb.Insert(index, code + "\n");
    }

    /// <summary>
    /// Insert code inside main()'s body, right before its 'endfunction'. Use
    /// this for init/trigger-registration calls that need to run at map
    /// startup. Safe to call from multiple injectors — each call re-locates
    /// the (shifting) endfunction position, so calls stack up in the order
    /// they were inserted.
    /// </summary>
    public void InsertIntoMainBody(string code)
    {
        var script = _sb.ToString();
        var mainIndex = script.IndexOf("function main takes nothing returns nothing", StringComparison.Ordinal);

        if (mainIndex == -1)
        {
            throw new InvalidOperationException("Could not find 'function main' in JASS script");
        }

        var endFunctionIndex = script.IndexOf("endfunction", mainIndex, StringComparison.Ordinal);

        if (endFunctionIndex == -1)
        {
            throw new InvalidOperationException("Could not find 'endfunction' for main function");
        }

        _sb.Insert(endFunctionIndex, code);
    }
}