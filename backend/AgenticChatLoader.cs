using System.Reflection;
using System.Runtime.Loader;

/// <summary>
/// Loads chat from DBADashWebView.AgenticChat.dll via reflection so the main app
/// never references Trimble (fixes IIS 500.30 when Trimble DLL is missing/broken).
/// </summary>
public static class AgenticChatLoader
{
    private const string PluginFile = "DBADashWebView.AgenticChat.dll";
    private const string TrimbleFile = "Trimble.AgenticChat.Core.dll";
    private const string BootstrapType = "DBADashWebView.AgenticChat.AgenticChatBootstrap";
    private static bool _loaded;
    private static Assembly? _pluginAssembly;
    private static AgenticChatPluginLoadContext? _loadContext;

    public static void TryConfigure(WebApplicationBuilder builder)
    {
        if (!IsChatRequested(builder.Configuration))
        {
            RegisterDisabled(builder, "AgenticChat:Enabled is false");
            return;
        }

        var baseDir = AppContext.BaseDirectory;
        var pluginPath = Path.Combine(baseDir, PluginFile);
        var trimblePath = Path.Combine(baseDir, TrimbleFile);

        if (!File.Exists(pluginPath))
        {
            RegisterDisabled(builder, $"{PluginFile} not found in site folder");
            return;
        }

        if (!File.Exists(trimblePath))
        {
            RegisterDisabled(builder, $"{TrimbleFile} not found in site folder");
            return;
        }

        if (!TryLoadPlugin(baseDir, pluginPath, trimblePath, out var pluginAssembly, out var error))
        {
            RegisterDisabled(builder, error ?? "Plugin load failed");
            return;
        }

        try
        {
            _pluginAssembly = pluginAssembly;

            var bootstrap = _pluginAssembly!.GetType(BootstrapType, throwOnError: true)!;
            // Must use WebApplicationBuilder overload — IServiceCollection/IConfiguration types
            // differ across AssemblyLoadContext and GetMethod(IServiceCollection, IConfiguration) returns null.
            var configure = bootstrap.GetMethod(
                "ConfigureServices",
                new[] { typeof(WebApplicationBuilder) });
            if (configure == null)
                throw new InvalidOperationException(
                    "AgenticChatBootstrap.ConfigureServices(WebApplicationBuilder) not found");

            configure.Invoke(null, new object[] { builder });

            _loaded = true;
            builder.Services.AddSingleton(new AgenticChatPluginStatus(true, null));
        }
        catch (Exception ex)
        {
            _loaded = false;
            _pluginAssembly = null;
            _loadContext = null;
            RegisterDisabled(builder, ex.InnerException?.Message ?? ex.Message);
            Console.Error.WriteLine($"[AgenticChat] Plugin configure failed: {ex}");
        }
    }

    public static void TryMapEndpoints(WebApplication app)
    {
        if (_loaded && _pluginAssembly != null)
        {
            try
            {
                var bootstrap = _pluginAssembly.GetType(BootstrapType, throwOnError: true)!;
                var map = bootstrap.GetMethod("MapEndpoints", new[] { typeof(WebApplication) });
                if (map != null)
                {
                    map.Invoke(null, new object[] { app });
                    return;
                }

                var mapHost = bootstrap.GetMethod("MapEndpointsForHost", new[] { typeof(object) });
                mapHost?.Invoke(null, new object[] { app });
                return;
            }
            catch (Exception ex)
            {
                app.Logger.LogError(ex, "Agentic Chat MapEndpoints failed");
            }
        }

        app.MapGet("/api/chat/health", (AgenticChatPluginStatus s) => Results.Ok(new
        {
            status = "disabled",
            enabled = false,
            reason = s.Reason,
            timestamp = DateTime.UtcNow
        }));
    }

    private static bool TryLoadPlugin(
        string baseDir,
        string pluginPath,
        string trimblePath,
        out Assembly? pluginAssembly,
        out string? error)
    {
        pluginAssembly = null;
        error = null;

        try
        {
            AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            AssemblyLoadContext.Default.LoadFromAssemblyPath(trimblePath);
            pluginAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(pluginPath);
            _loadContext = null;
            return true;
        }
        catch (Exception ex2)
        {
            error = ex2.InnerException?.Message ?? ex2.Message;
            Console.Error.WriteLine($"[AgenticChat] Default-context load failed: {ex2}");
        }

        try
        {
            _loadContext = new AgenticChatPluginLoadContext(baseDir, trimblePath, pluginPath);
            pluginAssembly = _loadContext.LoadPlugin();
            return true;
        }
        catch (Exception ex)
        {
            error = ex.InnerException?.Message ?? ex.Message;
            Console.Error.WriteLine($"[AgenticChat] Plugin load failed: {ex}");
            return false;
        }
    }

    private static Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
    {
        var name = new AssemblyName(args.Name).Name;
        if (name == null)
            return null;

        var candidate = Path.Combine(AppContext.BaseDirectory, name + ".dll");
        return File.Exists(candidate)
            ? AssemblyLoadContext.Default.LoadFromAssemblyPath(candidate)
            : null;
    }

    private static bool IsChatRequested(IConfiguration config) =>
        config.GetValue("AgenticChat:Enabled", true);

    private static void RegisterDisabled(WebApplicationBuilder builder, string reason)
    {
        _loaded = false;
        builder.Services.AddSingleton(new AgenticChatPluginStatus(false, reason));
    }
}

public sealed record AgenticChatPluginStatus(bool Enabled, string? Reason);
