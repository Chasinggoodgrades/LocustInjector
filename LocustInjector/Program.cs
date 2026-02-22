using System.Reflection;
using System.Runtime.Loader;

public static class Program
{
    public static void Main(string[] args)
    {
        // Set up assembly resolver to find DLLs in libs folder
        AssemblyLoadContext.Default.Resolving += (context, assemblyName) =>
        {
            var libsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libs", $"{assemblyName.Name}.dll");
            if (File.Exists(libsPath))
            {
                return context.LoadFromAssemblyPath(libsPath);
            }
            return null;
        };

        try
        {
            var exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var mapsFolder = Path.Combine(exeDirectory, "Maps");
            var locustMapsFolder = Path.Combine(exeDirectory, "LocustMaps");
            var tempOutputPath = Path.Combine(exeDirectory, "temp_output");

            List<string> mapsToProcess = new List<string>();

            // Check if a file was dragged onto the executable
            if (args.Length > 0 && File.Exists(args[0]))
            {
                mapsToProcess.Add(args[0]);
                Console.WriteLine($"Processing dropped file: {Path.GetFileName(args[0])}");
            }
            else
            {
                // Process all maps from the Maps folder
                if (!Directory.Exists(mapsFolder))
                {
                    Directory.CreateDirectory(mapsFolder);
                    Console.WriteLine($"Created 'Maps' folder at: {mapsFolder}");
                    Console.WriteLine("Please place your .w3x or .w3m map files in this folder.");
                    Console.WriteLine("\nYou can also drag and drop a map file onto this executable.");
                    Console.WriteLine("\nPress any key to exit...");
                    Console.ReadKey();
                    return;
                }

                var mapFiles = Directory.GetFiles(mapsFolder, "*.w3x")
                    .Concat(Directory.GetFiles(mapsFolder, "*.w3m"))
                    .ToList();

                if (mapFiles.Count == 0)
                {
                    Console.WriteLine("No map files found in the Maps folder.");
                    Console.WriteLine("Supported formats: .w3x, .w3m");
                    Console.WriteLine("\nYou can also drag and drop a map file onto this executable.");
                    Console.WriteLine("\nPress any key to exit...");
                    Console.ReadKey();
                    return;
                }

                mapsToProcess.AddRange(mapFiles);
                Console.WriteLine($"Found {mapFiles.Count} map(s) to process.\n");
            }

            // Create output folder
            Directory.CreateDirectory(locustMapsFolder);

            int processed = 0;
            int failed = 0;

            foreach (var mapPath in mapsToProcess)
            {
                try
                {
                    Console.WriteLine($"{'='*60}");
                    Console.WriteLine($"Processing: {Path.GetFileName(mapPath)}");
                    Console.WriteLine($"{'='*60}");

                    // Extract
                    MPQExtractor.ExtractJFile(mapPath, tempOutputPath);
                    
                    // Inject Locust
                    LocustInjector.BeginInjection(tempOutputPath);
                    
                    // Build output filename
                    var fileName = Path.GetFileNameWithoutExtension(mapPath);
                    var extension = Path.GetExtension(mapPath);
                    var outputFileName = $"{fileName}_Locust{extension}";
                    var outputPath = Path.Combine(locustMapsFolder, outputFileName);
                    
                    // Inject back into map
                    MPQInjector.InjectJFile(mapPath, tempOutputPath, outputPath);
                    
                    Console.WriteLine($"\nSuccessfully processed: {outputFileName}");
                    processed++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nFailed to process {Path.GetFileName(mapPath)}");
                    Console.WriteLine($"Error: {ex.Message}");
                    failed++;
                }
                finally
                {
                    // Clean up temp output folder after each map
                    if (Directory.Exists(tempOutputPath))
                    {
                        Directory.Delete(tempOutputPath, true);
                    }
                }

                Console.WriteLine();
            }

            Console.WriteLine($"\n{'='*60}");
            Console.WriteLine($"=== Processing Complete ===");
            Console.WriteLine($"Successfully processed: {processed}");
            if (failed > 0)
            {
                Console.WriteLine($"Failed: {failed}");
            }
            Console.WriteLine($"Output folder: {locustMapsFolder}");
            Console.WriteLine($"{'='*60}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nFatal Error: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}