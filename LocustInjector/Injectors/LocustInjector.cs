public sealed class LocustInjector : IJassInjector
{
    private const string LocustAbilityCode = "'Aloc'";
    private const string BearFormAbilityCode = "'Abrf'";
    private const string TriggerInitUnit = "gg_trg_LocustInit";
    private const string TriggerEnterMap = "gg_trg_LocustEnter";

    public string Name => "Locust Injector";

    // These ultimately get verified at end of map injection. 
    public IEnumerable<string> RequiredTokens => new[]
    {
        "function Trig_LocustInit_Actions",
        "function Trig_LocustEnter_Actions",
        TriggerInitUnit,
        TriggerEnterMap,
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

    private static string GetGlobalDeclarations() => $@"
trigger {TriggerInitUnit} = null
trigger {TriggerEnterMap} = null";


    private static string GetMainCalls() => $@"    
call InitTrig_LocustInit()
call InitTrig_LocustEnter()
call Trig_LocustInit_Actions()
";


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
        call BlzSetUnitBooleanField(u, UNIT_BF_HERO_HIDE_HERO_DEATH_MESSAGE, true)

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
        call BlzSetUnitBooleanField(u, UNIT_BF_HERO_HIDE_HERO_DEATH_MESSAGE, true)
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