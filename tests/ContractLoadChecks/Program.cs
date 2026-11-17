using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;

if (args.Length < 2) throw new ArgumentException("Usage: ContractLoadChecks <legacy-contract.dll> <HudChecks.dll> [plugin.dll ...]");
var contractPath = Path.GetFullPath(args[0]);
var checksPath = Path.GetFullPath(args[1]);
AssemblyLoadContext.Default.Resolving += (_, name) =>
{
    foreach (var directory in new[] { Path.GetDirectoryName(checksPath)! }.Concat(args.Skip(2).Select(p => Path.GetDirectoryName(Path.GetFullPath(p))!)))
    {
        var path = Path.Combine(directory, name.Name + ".dll");
        if (File.Exists(path)) return AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
    }
    return null;
};
// This bootstrap has no menu references: preload the old export before the CLR
// can resolve any of the check assembly's references to the current contract.
var legacy = AssemblyLoadContext.Default.LoadFromAssemblyPath(contractPath);
if (legacy.GetType("Sws2Cs2Menus.Contracts.IMenuService", throwOnError: true)!.GetProperty("IsCustomHudAvailable") is not null)
    throw new InvalidOperationException("Fixture must represent the old contract without the HUD member");
Console.WriteLine($"Preloaded legacy export: {legacy.Location}");
try
{
    AssemblyLoadContext.Default.LoadFromAssemblyPath(checksPath).EntryPoint!.Invoke(null, [Array.Empty<string>()]);
}
catch (TargetInvocationException exception) when (exception.InnerException is not null)
{
    ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
}
if (typeof(Program).Assembly.GetReferencedAssemblies().Any(a => a.Name == "Sws2Cs2Menus.Contracts"))
    throw new InvalidOperationException("Bootstrap must not reference the menu contract");
foreach (var pluginPath in args.Skip(2))
{
    var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(pluginPath));
    var count = 0;
    foreach (var type in assembly.GetTypes().Where(t => !t.ContainsGenericParameters))
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        foreach (var method in type.GetMethods(flags).Cast<MethodBase>().Concat(type.GetConstructors(flags)))
        {
            if (method.ContainsGenericParameters || method.IsAbstract || method.GetMethodBody() is null) continue;
            // Compile actual plugin code against the selected legacy export without
            // invoking native CS2 operations. Missing member references fail here.
            RuntimeHelpers.PrepareMethod(method.MethodHandle);
            count++;
        }
    }
    Console.WriteLine($"JIT-checked {count} methods in {assembly.GetName().Name} against the legacy export.");
}
Console.WriteLine("Legacy export binary compatibility checks passed.");
