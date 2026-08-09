/// <summary>
/// Runs a set of IJassInjector instances against one map's war3map.j:
/// load once, apply every injector in order, verify, save once.
/// </summary>
public static class JassInjectionPipeline
{
    public static void Run(string outputPath, IReadOnlyList<IJassInjector> injectors)
    {
        Console.WriteLine("\nReading JASS script...");
        var script = JassScript.Load(outputPath);
        var originalLength = script.Length;
        Console.WriteLine($"  Script length: {originalLength} characters");

        foreach (var injector in injectors)
        {
            Console.WriteLine($"Injecting {injector.Name}...");
            injector.Inject(script);
            Console.WriteLine($"  Script length after {injector.Name}: {script.Length} characters");
        }

        Console.WriteLine("Verifying injected content...");
        VerifyAll(script, injectors);

        if (script.Length <= originalLength)
        {
            throw new InvalidOperationException(
                "Modified script is not longer than the original — injection likely failed silently.");
        }

        Console.WriteLine("Saving modified JASS script...");
        script.Save();
        Console.WriteLine("All injections complete!");
    }

    private static void VerifyAll(JassScript script, IReadOnlyList<IJassInjector> injectors)
    {
        var allPassed = true;

        foreach (var injector in injectors)
        {
            foreach (var token in injector.RequiredTokens)
            {
                var found = script.Contains(token);
                Console.WriteLine($"  [{(found ? "OK" : "MISSING")}] {injector.Name}: {token}");
                if (!found) allPassed = false;
            }
        }

        if (!allPassed)
        {
            throw new InvalidOperationException(
                "One or more injected tokens are missing from the written JASS file. " +
                "Check the output above for details.");
        }
    }
}