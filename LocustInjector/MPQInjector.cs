using War3Net.IO.Mpq;

public static class MPQInjector
{
    public static void InjectJFile(string mapPath, string extractedJassPath, string outputMapPath)
    {
        if (!File.Exists(mapPath))
        {
            throw new FileNotFoundException($"Map file not found: {mapPath}");
        }

        Console.WriteLine($"\nOpening archive for injection: {Path.GetFileName(mapPath)}");

        var modifiedJassPath = Path.Combine(extractedJassPath, "war3map.j");
        if (!File.Exists(modifiedJassPath))
        {
            throw new FileNotFoundException($"Modified JASS file not found: {modifiedJassPath}");
        }

        Console.WriteLine($"Creating modified map: {Path.GetFileName(outputMapPath)}");

        // Open original archive and read all files
        using (var originalArchive = MpqArchive.Open(mapPath))
        {
            var modifiedJass = File.ReadAllText(modifiedJassPath);
            
            // Determine the correct path in the archive
            var jassFileName = originalArchive.FileExists("Scripts\\war3map.j") 
                ? "Scripts\\war3map.j" 
                : "war3map.j";

            Console.WriteLine($"Building new archive with modified {jassFileName}...");

            // Create new archive builder
            var builder = new MpqArchiveBuilder(originalArchive);
            
            // Add/Replace the modified JASS file (don't remove first, just overwrite)
            var jassBytes = System.Text.Encoding.UTF8.GetBytes(modifiedJass);
            var stream = new MemoryStream(jassBytes);
            stream.Position = 0;
            
            var mpqFile = MpqFile.New(stream, jassFileName);
            builder.AddFile(mpqFile);
            
            // Save to output file
            Console.WriteLine("Saving modified archive...");
            builder.SaveTo(outputMapPath);
            
            // Clean up
            stream.Dispose();
        }

        Console.WriteLine($"Saved to: {outputMapPath}");
    }
}