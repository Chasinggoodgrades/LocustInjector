/// <summary>
/// To add a new injector:
///   1. Create a class that implements this interface.
///   2. In Inject(), use the JassScript helpers to add your globals, your
///      function/trigger definitions, and any startup calls your feature
///      needs — call script.InsertIntoMainBody(...) yourself rather than   
///      relying on another injector to kick things off for you. That's what
///      keeps these composable: each injector should work correctly even if
///      it's the only one registered.
///   3. (Optional) Add any tokens to RequiredTokens that you want to verify are present in the final script.
///   4. Register an instance of your injector in  Injectors list.
/// </summary>
public interface IJassInjector
{
    /// <summary>Name, used for console logging.</summary>
    string Name { get; }

    /// <summary>Apply this injector's changes to the shared script buffer.</summary>
    void Inject(JassScript script);

    /// <summary>
    /// Used purely for post verification
    /// </summary>
    IEnumerable<string> RequiredTokens => Array.Empty<string>();
}