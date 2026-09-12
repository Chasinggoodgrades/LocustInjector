public static class InjectionList
{
    // Add new injectors here.. This is what ultimately gets added to the program pipeline. Order matters.
    public static IReadOnlyList<IJassInjector> Injectors { get; } = new IJassInjector[]
    {
        new UnitSelectionInjector(), // Moved this first so that in the future.. I can pull from the GetPlayerColorString function in CmdCenter if wanted..
        new LocustInjector(),
        new AchesCommandCenterInjector(),
    };
}