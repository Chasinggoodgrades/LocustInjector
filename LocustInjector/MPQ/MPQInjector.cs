using System.Linq;
using War3Net.IO.Mpq;

[Flags]
public enum InjectionTargets
{
    None = 0,
    Jass = 1,
    Wts = 2,
    Misc = 4,
    All = Jass | Wts | Misc,
}

public static class MPQInjector
{
    public static void InjectJFile(string mapPath, string extractedJassPath, string outputMapPath)
    {
        var filesToWrite = BuildFilesToWrite(mapPath, extractedJassPath, InjectionTargets.All, out var jassFileName);

        Console.WriteLine("Saving modified archive (in-place patch)...");
        MpqInPlacePatcher.Patch(mapPath, outputMapPath, filesToWrite);

        var debugFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Debug");
        Directory.CreateDirectory(debugFolder);
        var mapName = Path.GetFileNameWithoutExtension(outputMapPath);
        using (var verifyArchive = MpqArchive.Open(outputMapPath))
        {
            verifyArchive.AddFileName(jassFileName);
            var extractedPath = Path.Combine(debugFolder, $"war3map_injected_{mapName}.j");
            using var verifyStream = verifyArchive.OpenFile(jassFileName);
            using var outFile = File.Create(extractedPath);
            verifyStream.CopyTo(outFile);
            Console.WriteLine($"  Injected JASS extracted to: {extractedPath}");
        }

        Console.WriteLine($"Saved to: {outputMapPath}");
    }

    public static Dictionary<string, byte[]> BuildFilesToWrite(string mapPath, string extractedJassPath, InjectionTargets targets, out string jassFileName)
    {
        if (!File.Exists(mapPath))
            throw new FileNotFoundException($"Map file not found: {mapPath}");

        var modifiedJassPath = Path.Combine(extractedJassPath, "war3map.j");
        if (!File.Exists(modifiedJassPath))
            throw new FileNotFoundException($"Modified JASS file not found: {modifiedJassPath}");

        Console.WriteLine($"\nOpening archive for injection: {Path.GetFileName(mapPath)}");

        var modifiedJass = WtsIO.Decode(File.ReadAllBytes(modifiedJassPath));
        var mapNameStringNumber = ExtractMapNameStringNumber(modifiedJass);


        var filesToWrite = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);


        using (var originalArchive = MpqArchive.Open(mapPath, loadListFile: true))
        {
            var jassCandidates = new[] { "Scripts\\war3map.j", "war3map.j" };
            jassFileName = jassCandidates.FirstOrDefault(originalArchive.FileExists)
                ?? throw new FileNotFoundException("Could not locate war3map.j inside the archive (checked root and Scripts\\ paths).");

            if (targets.HasFlag(InjectionTargets.Jass))
            {
                Console.WriteLine($"Injecting {jassFileName}...");
                filesToWrite[jassFileName] = WtsIO.Encode(modifiedJass);
            }
            else
            {
                Console.WriteLine($"[isolated test] Skipping {jassFileName} injection.");
            }

            var wtsFileName = WTSInjector.TargetWtsFileName;
            if (targets.HasFlag(InjectionTargets.Wts) && originalArchive.FileExists(wtsFileName))
            {
                string wtsContent;
                using (var wtsStream = originalArchive.OpenFile(wtsFileName))
                using (var ms = new MemoryStream())
                {
                    wtsStream.CopyTo(ms);
                    wtsContent = WtsIO.Decode(ms.ToArray());
                }

                var transformed = mapNameStringNumber != null
                    ? WTSInjector.InjectMapNameSuffix(wtsContent, mapNameStringNumber)
                    : WTSInjector.InjectMapNameSuffix(wtsContent);

                if (transformed != wtsContent)
                {
                    Console.WriteLine($"Injecting suffix into STRING {mapNameStringNumber ?? "3"} of {wtsFileName}...");
                    filesToWrite[wtsFileName] = WtsIO.Encode(transformed);
                }
            }

            if (targets.HasFlag(InjectionTargets.Misc))
            {
                var miscFileName = MiscInjector.TargetMiscFileName;
                var miscExists = originalArchive.FileExists(miscFileName);
                string? miscContent = null;
                if (miscExists)
                {
                    using var miscStream = originalArchive.OpenFile(miscFileName);
                    using var reader = new StreamReader(miscStream, System.Text.Encoding.Latin1);
                    miscContent = reader.ReadToEnd();
                }

                var transformedMisc = MiscInjector.InjectMaxUnitSpeed(miscContent);
                if (transformedMisc != miscContent)
                {
                    Console.WriteLine(miscExists
                        ? $"Injecting MaxUnitSpeed into {miscFileName}..."
                        : $"Creating {miscFileName} with MaxUnitSpeed...");
                    filesToWrite[miscFileName] = System.Text.Encoding.Latin1.GetBytes(transformedMisc);
                }
            }
        }

        return filesToWrite;
    }

    /// <summary>
    /// Scans the JASS source for a call like SetMapName("TRIGSTR_005") and returns the
    /// numeric string index (e.g. "5") used to locate the map name entry in war3map.wts.
    /// Returns null if no such call is found and defaults to parm in <see cref="WTSInjector.InjectMapNameSuffix"/>
    /// </summary>
    private static string? ExtractMapNameStringNumber(string jassContent)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            jassContent,
            @"SetMapName\s*\(\s*""TRIGSTR_0*(\d+)""\s*\)");

        if (!match.Success)
            return null;

        var number = match.Groups[1].Value;
        Console.WriteLine($"Found SetMapName TRIGSTR_{number} in JASS.");
        return number;
    }
}