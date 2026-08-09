public sealed class AchesCommandCenterInjector : IJassInjector
{
    public string Name => "Aches Command Center";

    // These ultimately get verified at end of map injection. 
    public IEnumerable<string> RequiredTokens => new[]
    {
        "LIBRARY_CommandsManager",
        "function RegisterCommand",
        "function CommandsManager___InitCommands",
        "library CommandsManager ends",
    };

    public void Inject(JassScript script)
    {
        // Globals get merged into the existing globals block
        script.InsertBeforeEndGlobals(GetAchesCommandCenterCode());
        
        // Make the functions just above the main function.. Easy to reference and insert into script
        script.InsertBeforeMainFunction(GetAchesCommandCenterCode2());

        // Init function calls get inserted into the main function
        script.InsertIntoMainBody(GetMainCalls());
    }

    private static string GetMainCalls() => $@"    
call ExecuteFunc(""CommandsManager___InitCommands"")
";

    private static string GetAchesCommandCenterCode()
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Jass", "CommandsManager_Globals.j"));
    }

    private static string GetAchesCommandCenterCode2()
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Jass", "CommandsManager_Library.j"));
    }
}