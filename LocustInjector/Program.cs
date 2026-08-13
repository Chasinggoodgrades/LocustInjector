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

            // This checks if u drag and dropped a file onto the exe
            if (args.Length > 0 && File.Exists(args[0]))
            {
                mapsToProcess.Add(args[0]);
                Console.WriteLine($"Processing dropped file: {Path.GetFileName(args[0])}");
            }
            else
            {
                // Grab maps from Maps folder 
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

            // Create output folder and put em in there
            Directory.CreateDirectory(locustMapsFolder);

            int processed = 0;
            int failed = 0;
            var failedMaps = new List<string>();
            var separator = new string('=', 60);

            foreach (var mapPath in mapsToProcess)
            {
                try
                {
                    Console.WriteLine(separator);
                    Console.WriteLine($"Processing: {Path.GetFileName(mapPath)}");
                    Console.WriteLine(separator);

                    // Extract
                    MPQExtractor.ExtractJFile(mapPath, tempOutputPath);

                    // Run every registered JASS injector against the extracted script
                    JassInjectionPipeline.Run(tempOutputPath, InjectionList.Injectors);

                    // Build output filename
                    var fileName = Path.GetFileNameWithoutExtension(mapPath);
                    var extension = Path.GetExtension(mapPath);
                    var outputFileName = $"{fileName}_vAches{extension}";
                    var outputPath = Path.Combine(locustMapsFolder, outputFileName);

                    // Inject back into map (also patches STRING 3 in war3map.wts if present)
                    MPQInjector.InjectJFile(mapPath, tempOutputPath, outputPath);

                    Console.WriteLine($"\nSuccessfully processed: {outputFileName}");
                    processed++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nFailed to process {Path.GetFileName(mapPath)}");
                    Console.WriteLine($"Error: {ex.Message}");
                    failed++;
                    failedMaps.Add(Path.GetFileName(mapPath));
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

            Console.WriteLine();
            Console.WriteLine(separator);
            Console.WriteLine($"=== Processing Complete ===");
            Console.WriteLine($"Successfully processed: {processed}");
            if (failed > 0)
            {
                Console.WriteLine($"Failed: {failed}");
                Console.WriteLine("Failed maps:");
                foreach (var name in failedMaps)
                {
                    Console.WriteLine($"  - {name}");
                }
            }
            Console.WriteLine($"Output folder: {locustMapsFolder}");
            Console.WriteLine(separator);
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