using System.Text;

public static class LocustInjector
{
    private const string LocustAbilityCode = "'Aloc'";
    private const string BearFormAbilityCode = "'Abrf'";
    private const string TriggerInitUnit = "gg_trg_LocustInit";
    private const string TriggerEnterMap = "gg_trg_LocustEnter";

    public static void BeginInjection(string outputPath)
    {
        var jassFilePath = Path.Combine(outputPath, "war3map.j");
        
        if (!File.Exists(jassFilePath))
        {
            Console.WriteLine($"\nError: war3map.j not found in {outputPath}");
            return;
        }

        Console.WriteLine("\nReading JASS script...");
        var originalScript = File.ReadAllText(jassFilePath);

        Console.WriteLine("Injecting Locust triggers...");
        var modifiedScript = InjectLocustAbility(originalScript);

        Console.WriteLine("Saving modified JASS script...");
        File.WriteAllText(jassFilePath, modifiedScript);

        Console.WriteLine("Locust injection complete!");
    }

    public static string InjectLocustAbility(string script)
    {
        var sb = new StringBuilder(script);

        // 1. Add trigger variables to globals
        InjectGlobalTriggers(sb);

        // 2. Add trigger creation and initialization functions
        InjectTriggerFunctions(sb);

        // 3. Call initialization in main function
        InjectMainCall(sb);

        return sb.ToString();
    }

    private static void InjectGlobalTriggers(StringBuilder sb)
    {
        var globalDeclarations = $@"
trigger {TriggerInitUnit} = null
trigger {TriggerEnterMap} = null";

        var endGlobalsIndex = sb.ToString().IndexOf("endglobals");
        
        if (endGlobalsIndex == -1)
        {
            throw new InvalidOperationException("Could not find 'endglobals' in JASS script");
        }

        sb.Insert(endGlobalsIndex, globalDeclarations + "\n");
    }

    private static void InjectTriggerFunctions(StringBuilder sb)
    {
        var triggerCode = GenerateTriggerCode();
        
        var mainFunctionIndex = sb.ToString().IndexOf("function main");
        
        if (mainFunctionIndex == -1)
        {
            throw new InvalidOperationException("Could not find 'function main' in JASS script");
        }

        sb.Insert(mainFunctionIndex, triggerCode + "\n");
    }

    private static void InjectMainCall(StringBuilder sb)
    {
        var initCalls = $@"    
call InitTrig_LocustInit()
call InitTrig_LocustEnter()
call Trig_LocustInit_Actions()
";

        var script = sb.ToString();
        var mainFunctionIndex = script.IndexOf("function main takes nothing returns nothing");
        
        if (mainFunctionIndex == -1)
        {
            throw new InvalidOperationException("Could not find 'function main' in JASS script");
        }

        // Find the endfunction for main
        var endFunctionIndex = script.IndexOf("endfunction", mainFunctionIndex);
        
        if (endFunctionIndex == -1)
        {
            throw new InvalidOperationException("Could not find 'endfunction' for main function");
        }

        // Insert before the endfunction
        sb.Insert(endFunctionIndex, initCalls);
    }

    private static string GenerateTriggerCode()
    {
        return $@"
//===========================================================================
// Trigger: LocustInit (Intention is to make older maps feel a smidge more modern)
//===========================================================================
function Trig_LocustInit_Actions takes nothing returns nothing
    local group g = CreateGroup()
    local unit u
    local player owner
    call GroupEnumUnitsInRect(g, GetPlayableMapRect(), null)
    loop
        set u = FirstOfGroup(g)
        exitwhen u == null
        set owner = GetOwningPlayer(u)
        
        if GetUnitAbilityLevel(u, {LocustAbilityCode}) == 0 then
            // Check if unit is owned by a user-controlled player
                // Apply half-locust for player units
                call UnitAddAbility(u, {LocustAbilityCode})
                call ShowUnit(u, false)
                call UnitRemoveAbility(u, {LocustAbilityCode})
                call ShowUnit(u, true)
                
                // Add Bear Form, cast it, then remove it
                call UnitAddAbility(u, {BearFormAbilityCode})
                call IssueImmediateOrder(u, ""bearform"")
                call UnitRemoveAbility(u, {BearFormAbilityCode})
        endif
        
        call GroupRemoveUnit(g, u)
    endloop
    call DestroyGroup(g)
    set g = null
endfunction

function InitTrig_LocustInit takes nothing returns nothing
    set {TriggerInitUnit} = CreateTrigger()
    call TriggerAddAction({TriggerInitUnit}, function Trig_LocustInit_Actions)
endfunction

//===========================================================================
// Trigger: LocustEnter (Intention is to make older maps feel a smidge more modern)
//===========================================================================
function Trig_LocustEnter_Conditions takes nothing returns boolean
    return GetUnitAbilityLevel(GetTriggerUnit(), {LocustAbilityCode}) == 0
endfunction

function Trig_LocustEnter_Actions takes nothing returns nothing
    local unit u = GetTriggerUnit()
    local player owner = GetOwningPlayer(u)
    
    // Check if unit is owned by a user-controlled player
        // Apply half-locust for player units
        call UnitAddAbility(u, {LocustAbilityCode})
        call ShowUnit(u, false)
        call UnitRemoveAbility(u, {LocustAbilityCode})
        call ShowUnit(u, true)
        
        // Add Bear Form, cast it, then remove it
        call UnitAddAbility(u, {BearFormAbilityCode})
        call IssueImmediateOrder(u, ""bearform"")
        call UnitRemoveAbility(u, {BearFormAbilityCode})
endfunction

function InitTrig_LocustEnter takes nothing returns nothing
    set {TriggerEnterMap} = CreateTrigger()
    call TriggerRegisterEnterRectSimple({TriggerEnterMap}, GetPlayableMapRect())
    call TriggerAddCondition({TriggerEnterMap}, Condition(function Trig_LocustEnter_Conditions))
    call TriggerAddAction({TriggerEnterMap}, function Trig_LocustEnter_Actions)
endfunction
";
    }
}