using System.Collections.Immutable;
using War3Net.IO.Mpq;

public static class MPQInjector
{
    public static void InjectJFile(string mapPath, string extractedJassPath, string outputMapPath)
    {
        if (!File.Exists(mapPath))
            throw new FileNotFoundException($"Map file not found: {mapPath}");

        var modifiedJassPath = Path.Combine(extractedJassPath, "war3map.j");
        if (!File.Exists(modifiedJassPath))
            throw new FileNotFoundException($"Modified JASS file not found: {modifiedJassPath}");

        Console.WriteLine($"\nOpening archive for injection: {Path.GetFileName(mapPath)}");

        /* Read the modified JASS using Latin1 — WC3 JASS files are Windows-1252 (ANSI). 
         * Latin1 maps bytes 0-255 directly to the same Unicode code points, so it is
         * perfectly lossless for any ANSI-encoded file. Using UTF-8 here corrupts maps
         * whose JASS contains characters above byte 127. */
        var modifiedJass = File.ReadAllText(modifiedJassPath, System.Text.Encoding.Latin1);


        var mapNameStringNumber = ExtractMapNameStringNumber(modifiedJass);

        // All MemoryStreams must remain alive until after SaveTo — MpqFile.New may hold
        // a reference to the stream rather than eagerly reading its contents.
        var streamsToDispose = new List<MemoryStream>();
        try
        {
            using var originalArchive = MpqArchive.Open(mapPath, loadListFile: true);

            int known = 0, other = 0;
            foreach (var f in originalArchive.GetMpqFiles())
            {
                if (f is MpqKnownFile k)
                {
                    known++;
                    if (k.FileName.Contains("war3map.j", StringComparison.OrdinalIgnoreCase) ||
                        k.FileName.Contains("listfile", StringComparison.OrdinalIgnoreCase) ||
                        k.FileName.Contains("attributes", StringComparison.OrdinalIgnoreCase))
                        Console.WriteLine($"  {k.FileName}");
                }
                else other++;
            }
            Console.WriteLine($"Known files: {known}, Other: {other}");

            var jassCandidates = new[] { "Scripts\\war3map.j", "war3map.j" };

            var jassFileName = jassCandidates.FirstOrDefault(originalArchive.FileExists);

            if (jassFileName == null)

                throw new FileNotFoundException("Could not locate war3map.j inside the archive (checked root and Scripts\\ paths).");



            Console.WriteLine($"Injecting {jassFileName}...");

            // Read and transform war3map.wts (if present) up front, in the same pass,
            // so the archive is only ever opened and rebuilt once.
            string? modifiedWts = null;
            var wtsFileName = WTSInjector.TargetWtsFileName;
            if (originalArchive.FileExists(wtsFileName))
            {
                string wtsContent;
                using (var wtsStream = originalArchive.OpenFile(wtsFileName))
                using (var reader = new StreamReader(wtsStream, System.Text.Encoding.Latin1))
                {
                    wtsContent = reader.ReadToEnd();
                }

                var transformed = mapNameStringNumber != null
                    ? WTSInjector.InjectMapNameSuffix(wtsContent, mapNameStringNumber)
                    : WTSInjector.InjectMapNameSuffix(wtsContent);
                if (transformed != wtsContent)
                {
                    Console.WriteLine($"Injecting suffix into STRING {mapNameStringNumber ?? "3"} of {wtsFileName}...");
                    modifiedWts = transformed;
                }
            }

            // Read and transform war3mapMisc.txt (if present) up front as well. If the
            // file doesn't exist at all, treat it as empty content so a new one is created.
            string? modifiedMisc = null;
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
                modifiedMisc = transformedMisc;
            }

            // Use an empty builder and manually copy every file from the original archive,
            // skipping only the JASS (and, if modified, WTS) entries. This avoids the HashSet
            // deduplication path in MpqArchiveBuilder(MpqArchive) that was silently keeping
            // both the old and new entries in the output archive.
            var builder = new MpqArchiveBuilder();
            foreach (var mpqFile in originalArchive.GetMpqFiles())
            {
                if (mpqFile is MpqKnownFile knownFile &&
                    (string.Equals(knownFile.FileName, jassFileName, StringComparison.OrdinalIgnoreCase) ||
                     (modifiedWts != null && string.Equals(knownFile.FileName, wtsFileName, StringComparison.OrdinalIgnoreCase)) ||
                     (modifiedMisc != null && string.Equals(knownFile.FileName, miscFileName, StringComparison.OrdinalIgnoreCase))))
                    continue;

                builder.AddFile(mpqFile, mpqFile.TargetFlags);
            }

            var jassBytes = System.Text.Encoding.Latin1.GetBytes(modifiedJass);
            var jassStream = new MemoryStream(jassBytes);
            streamsToDispose.Add(jassStream);
            builder.AddFile(MpqFile.New(jassStream, jassFileName));

            if (modifiedWts != null)
            {
                var wtsBytes = System.Text.Encoding.Latin1.GetBytes(modifiedWts);
                var wtsStream = new MemoryStream(wtsBytes);
                streamsToDispose.Add(wtsStream);
                builder.AddFile(MpqFile.New(wtsStream, wtsFileName));
            }

            if (modifiedMisc != null)
            {
                var miscBytes = System.Text.Encoding.Latin1.GetBytes(modifiedMisc);
                var miscStream = new MemoryStream(miscBytes);
                streamsToDispose.Add(miscStream);
                builder.AddFile(MpqFile.New(miscStream, miscFileName));
            }

            // Safety net: if any (name hash, locale) pair ends up duplicated in the file list
            // we're about to save, MpqArchive silently keeps only one copy of it (see the bug
            // fixed above) — meaning either the injection didn't take effect, or something else
            // about this specific map's layout wasn't accounted for. Fail loudly here instead of
            // producing a map that looks fine to this tool but won't load in Warcraft 3.
            var duplicateGroups = builder.ModifiedFiles
                .GroupBy(f => (f.Name, f.Locale))
                .Where(g => g.Count() > 1)
                .ToList();
            if (duplicateGroups.Count > 0)
            {
                var names = string.Join(", ", duplicateGroups.Select(g => $"0x{g.Key.Name:X16}"));
                throw new InvalidOperationException(
                    $"Refusing to save: {duplicateGroups.Count} file name hash(es) would be duplicated in the output archive ({names}). " +
                    "This means an injected file (war3map.j / war3map.wts / war3mapMisc.txt) didn't correctly replace the original entry — " +
                    "saving anyway would silently produce a map Warcraft 3 may fail to load.");
            }

            Console.WriteLine("Saving modified archive...");

            builder.SaveTo(outputMapPath);

            // Extract the injected JASS back out via War3Net for independent verification —
            // compare Debug\war3map_injected_*.j against Debug\war3map_modified_*.j to
            // confirm the content is correct without relying on a third-party MPQ editor.
            var debugFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Debug");
            Directory.CreateDirectory(debugFolder);
            var mapName = Path.GetFileNameWithoutExtension(outputMapPath);
            using var verifyArchive = MpqArchive.Open(outputMapPath);
            verifyArchive.AddFileName(jassFileName);
            var extractedPath = Path.Combine(debugFolder, $"war3map_injected_{mapName}.j");
            using (var verifyStream = verifyArchive.OpenFile(jassFileName))
            using (var outFile = File.Create(extractedPath))
                verifyStream.CopyTo(outFile);
            Console.WriteLine($"  Injected JASS extracted to: {extractedPath}");
        }
        finally
        {
            foreach (var s in streamsToDispose)
                s.Dispose();
        }

        Console.WriteLine($"Saved to: {outputMapPath}");
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