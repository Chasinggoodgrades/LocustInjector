using System.Reflection;

public sealed class UnitSelectionInjector : IJassInjector
{
    public string Name => "Unit Selection Injector";

    // These ultimately get verified at end of map injection. 
    public IEnumerable<string> RequiredTokens => new[]
    {
        "function Trig_AddUnitSelectionEvent_Actions",
        "function InitTrig_AddUnitSelectionEvent",
        "function GetPlayerColorString",
        "gg_trg_AddUnitSelectionEvent",
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
call InitTrig_AddUnitSelectionEvent()
";

    private static string GetGlobalDeclarations()
    {
        return ReadEmbeddedJassResource("Selection_Globals.j");
    }

    private static string GenerateTriggerCode()
    {
        return ReadEmbeddedJassResource("Selection_Library.j");
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
