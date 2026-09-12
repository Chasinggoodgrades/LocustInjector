using System.Reflection;

public sealed class LocustInjector : IJassInjector
{
    public string Name => "Locust Injector";

    // These ultimately get verified at end of map injection. 
    public IEnumerable<string> RequiredTokens => new[]
    {
        "function Trig_LocustInit_Actions",
        "function Trig_LocustEnter_Actions",
        "gg_trg_LocustInit",
        "gg_trg_LocustEnter",
    };

    public void Inject(JassScript script)
    {
        // Add trigger variables to globals
        script.InsertBeforeEndGlobals(GetGlobalDeclarations());

        // Add trigger creation and initialization functions
        script.InsertBeforeMainFunction(GenerateTriggerCode());

        // Call initialization in main function
        script.InsertIntoMainBody(GetMainCalls());
    }

    private static string GetMainCalls() => $@"    
call InitTrig_LocustInit()
call InitTrig_LocustEnter()
call Trig_LocustInit_Actions()
";

    private static string GetGlobalDeclarations()
    {
        return ReadEmbeddedJassResource("Locust_Globals.j");
    }

    private static string GenerateTriggerCode()
    {
        return ReadEmbeddedJassResource("Locust_Library.j");
    }

    private static string ReadEmbeddedJassResource(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"{assembly.GetName().Name}.Jass.{fileName}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded JASS resource '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
