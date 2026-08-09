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

        // All MemoryStreams must remain alive until after SaveTo — MpqFile.New may hold
        // a reference to the stream rather than eagerly reading its contents.
        var streamsToDispose = new List<MemoryStream>();
        try
        {
            using var originalArchive = MpqArchive.Open(mapPath, loadListFile: true);

            var jassFileName = originalArchive.FileExists("Scripts\\war3map.j")
                ? "Scripts\\war3map.j"
                : "war3map.j";

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

                var transformed = WTSInjector.InjectMapNameSuffix(wtsContent);
                if (transformed != wtsContent)
                {
                    Console.WriteLine($"Injecting suffix into STRING 3 of {wtsFileName}...");
                    modifiedWts = transformed;
                }
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
                     (modifiedWts != null && string.Equals(knownFile.FileName, wtsFileName, StringComparison.OrdinalIgnoreCase))))
                    continue;

                builder.AddFile(mpqFile);
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
}