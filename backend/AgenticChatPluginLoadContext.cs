using System.Reflection;
using System.Runtime.Loader;

/// <summary>
/// Loads Trimble + plugin assemblies (isolated ALC works on macOS where Default.LoadFrom fails for Trimble).
/// </summary>
internal sealed class AgenticChatPluginLoadContext : AssemblyLoadContext
{
    private readonly string _pluginDir;
    private readonly string _trimblePath;
    private readonly string _pluginPath;

    public AgenticChatPluginLoadContext(string pluginDir, string trimblePath, string pluginPath)
        : base("AgenticChatPlugin", isCollectible: false)
    {
        _pluginDir = pluginDir;
        _trimblePath = trimblePath;
        _pluginPath = pluginPath;
        LoadFromAssemblyPath(trimblePath);
    }

    public Assembly LoadPlugin() => LoadFromAssemblyPath(_pluginPath);

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (assemblyName.Name == null)
            return null;

        if (IsSharedFrameworkAssembly(assemblyName.Name))
            return null;

        if (assemblyName.Name.Equals(
                Path.GetFileNameWithoutExtension(_trimblePath),
                StringComparison.OrdinalIgnoreCase))
            return LoadFromAssemblyPath(_trimblePath);

        var candidate = Path.Combine(_pluginDir, assemblyName.Name + ".dll");
        if (File.Exists(candidate))
            return LoadFromAssemblyPath(candidate);

        return null;
    }

    private static bool IsSharedFrameworkAssembly(string name) =>
        name.StartsWith("Microsoft.AspNetCore.", StringComparison.OrdinalIgnoreCase) ||
        name.StartsWith("Microsoft.Extensions.", StringComparison.OrdinalIgnoreCase) ||
        name.StartsWith("System.", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Microsoft.Extensions", StringComparison.OrdinalIgnoreCase);
}
