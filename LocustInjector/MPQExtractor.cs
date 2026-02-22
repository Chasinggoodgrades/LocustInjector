using War3Net.IO.Mpq;

public static class MPQExtractor
{
    public static void ExtractJFile(string mapPath, string outputPath)
    {
        if (!File.Exists(mapPath))
        {
            throw new FileNotFoundException($"Map file not found: {mapPath}");
        }

        Console.WriteLine($"\nOpening archive: {Path.GetFileName(mapPath)}");
        
        using var archive = MpqArchive.Open(mapPath);

        LoadListFile(archive, mapPath);
        DisplayArchiveContents(archive);
        ExtractJassScript(archive, outputPath);
    }

    private static void LoadListFile(MpqArchive archive, string mapPath)
    {
        // Look for listfile in the executable's directory
        var exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var listFilePath = Path.Combine(exeDirectory, "listfile.txt");
        
        if (!File.Exists(listFilePath))
            return;

        Console.WriteLine("Loading listfile...");
        
        var fileNames = File.ReadAllLines(listFilePath)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Trim());

        foreach (var fileName in fileNames)
        {
            archive.AddFileName(fileName);
        }
    }

    private static void DisplayArchiveContents(MpqArchive archive)
    {
        Console.WriteLine("\nFiles in archive:");
        
        var knownFiles = archive.Where(entry => !string.IsNullOrEmpty(entry.FileName)).ToList();
        
        if (knownFiles.Count == 0)
        {
            Console.WriteLine("  (No named files found - add listfile.txt to see file names)");
        }
        else
        {
            foreach (var entry in knownFiles)
            {
                Console.WriteLine($"  - {entry.FileName}");
            }
        }
    }

    private static void ExtractJassScript(MpqArchive archive, string outputPath)
    {
        var possibleLocations = new[] { "war3map.j", "Scripts\\war3map.j" };
        
        var jassFile = possibleLocations.FirstOrDefault(archive.FileExists);

        if (jassFile == null)
        {
            Console.WriteLine("\nwar3map.j not found in archive (checked root and Scripts folder).");
            return;
        }

        Console.WriteLine($"\nExtracting {jassFile}...");
        
        Directory.CreateDirectory(outputPath);
        
        var outputFilePath = Path.Combine(outputPath, "war3map.j");
        
        using (var mpqStream = archive.OpenFile(jassFile))
        using (var fileStream = File.Create(outputFilePath))
        {
            mpqStream.CopyTo(fileStream);
        }
        
        Console.WriteLine($"Saved to: {outputFilePath}");
    }
}