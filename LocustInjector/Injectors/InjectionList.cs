public static class InjectionList
{
    // Add new injectors here.. This is what ultimately gets added to the program pipeline. Order matters.
    public static IReadOnlyList<IJassInjector> Injectors { get; } = new IJassInjector[]
    {
        new LocustInjector(),
        new AchesCommandCenterInjector(),
    };
}