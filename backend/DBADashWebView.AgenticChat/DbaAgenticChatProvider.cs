namespace DBADashWebView.AgenticChat;

using System.Security.Claims;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using Trimble.AgenticChat.Core;
using Trimble.AgenticChat.Core.AgenticModels;
using Trimble.AgenticChat.Core.Interfaces;
using Trimble.AgenticChat.Core.Models;

/// <summary>
/// Implements the IAgenticChatProvider interface for the DBA Dashboard
/// Provides configuration, authentication tokens, and run-time context for the Chat UI
/// </summary>
public class DbaAgenticChatProvider : IAgenticChatProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AgenticChatConfiguration _config;
    private readonly ITrimbleIdentityTokenService _trimbleTokenService;
    private readonly ILogger<DbaAgenticChatProvider> _logger;

    public DbaAgenticChatProvider(
        IHttpContextAccessor httpContextAccessor,
        AgenticChatConfiguration config,
        ITrimbleIdentityTokenService trimbleTokenService,
        ILogger<DbaAgenticChatProvider> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _config = config;
        _trimbleTokenService = trimbleTokenService;
        _logger = logger;
    }

    /// <summary>
    /// Returns the initial ChatUiConfiguration with environment, agent, theme, locale, etc.
    /// </summary>
    public Task<ChatUiConfiguration> ProvideChatUiConfig()
    {
        try
        {
            var environment = string.Equals(_config.Environment, "Prod", StringComparison.OrdinalIgnoreCase)
                ? Environment.Prod
                : Environment.Stage;

            var theme = _config.Theme switch
            {
                "Dark" => Theme.Dark,
                _ => Theme.Light
            };

            var variant = _config.Variant switch
            {
                "Minimal" => ChatUiVariants.Minimal,
                "Narrow" => ChatUiVariants.Narrow,
                _ => ChatUiVariants.Full
            };

            var contentVariant = _config.ContentVariant switch
            {
                "AgentCards" => ContentVariants.AgentCards,
                _ => ContentVariants.Chat
            };

            var config = new ChatUiConfiguration
            {
                Environment = environment,
                AgentId = _config.AgentId,
                ThreadId = null,
                OnBeforeRunTimeout = _config.OnBeforeRunTimeoutMs,
                UiConfig = new UiConfig
                {
                    Theme = theme,
                    Variant = variant,
                    ContentVariant = contentVariant,
                    ChatInput = new ChatInput
                    {
                        Buttons = new List<ChatInputButton>(),
                        HideModelSelection = _config.HideModelSelection
                    }
                },
                Localization = new Localization
                {
                    Locale = _config.Locale
                }
            };

            _logger.LogInformation("Chat UI Configuration provided for agent: {AgentId}", _config.AgentId);
            return Task.FromResult(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error providing Chat UI configuration");
            throw;
        }
    }

    /// <summary>
    /// Returns a valid Trimble Identity bearer token for the current user
    /// </summary>
    public async Task<string> ProvideChatUiToken()
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                _logger.LogWarning("No HTTP context available");
                return null!;
            }

            var fromRequest = ResolveTrimbleAccessTokenFromRequest(httpContext);
            if (!string.IsNullOrEmpty(fromRequest))
            {
                _logger.LogInformation(
                    "Trimble chat token source={Source} (matches Agent Studio when using a user token from Assist)",
                    ResolveChatTokenSource(httpContext));
                return fromRequest;
            }

            var fromClientCredentials = await _trimbleTokenService.GetChatAccessTokenAsync();
            if (!string.IsNullOrEmpty(fromClientCredentials))
            {
                _logger.LogInformation(
                    "Trimble chat token source=client_credentials scope={Scope}. "
                    + "If embed output differs from Agent Studio, set AgenticChat:TrimbleAccessToken to your Assist user token.",
                    _trimbleTokenService.ActiveChatScope ?? _config.TrimbleChatScope);
                return fromClientCredentials;
            }

            var authHeader = httpContext.Request.Headers.Authorization.ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "No Trimble token: configure TrimbleClientId/TrimbleClientSecret or X-Trimble-Access-Token");
                return null!;
            }

            _logger.LogError(
                "Trimble token unavailable; refusing DBA JWT fallback (configure Trimble client credentials)");
            return null!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error providing Chat UI token");
            throw;
        }
    }

    private string? ResolveTrimbleAccessTokenFromRequest(HttpContext httpContext)
    {
        var fromHeader = httpContext.Request.Headers["X-Trimble-Access-Token"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(fromHeader))
            return fromHeader;

        if (!string.IsNullOrWhiteSpace(_config.TrimbleAccessToken))
            return _config.TrimbleAccessToken;

        return null;
    }

    /// <summary>
    /// Called before each agent run. Injects run context and optional local runtime tools.
    /// </summary>
    public Task<OnBeforeRunConfig> ProvideRunConfig(string agentId)
    {
        try
        {
            if (_config.AgentStudioParityMode)
            {
                _logger.LogInformation(
                    "Run config for agent {AgentId}: Agent Studio parity (no local tools, no extra run context)",
                    agentId);
                return Task.FromResult(new OnBeforeRunConfig());
            }

            var displayName = ResolveDisplayName();
            var guide = BuildAgentRunContextGuide(displayName);

            LocalTools? tools = null;
            if (_config.RegisterLocalRuntimeTools)
            {
                tools = new LocalTools { RunTime = CreateLocalRuntimeTools() };
                _logger.LogDebug("Local runtime tools registered for agent {AgentId}", agentId);
            }

            var contextObjects = new List<ContextObject>
            {
                new() { Description = "Current User", Value = displayName },
                new() { Description = "Application", Value = "DBA Dashboard" },
                new() { Description = "Timestamp", Value = DateTime.UtcNow.ToString("O") }
            };

            var context = new OnBeforeRunConfig
            {
                Tools = tools,
                RunContext = new RunContext { Context = contextObjects }
            };

            if (!string.IsNullOrWhiteSpace(guide))
            {
                context.RunContext!.Context!.Add(new ContextObject
                {
                    Description = "dbadash tool protocol",
                    Value = guide
                });
            }

            _logger.LogInformation(
                "Run config for agent {AgentId}, user {UserName}, localTools={LocalTools}",
                agentId,
                displayName,
                _config.RegisterLocalRuntimeTools);

            return Task.FromResult(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error providing run configuration for agent {AgentId}", agentId);
            throw;
        }
    }

    /// <summary>
    /// Called when the UI reports an expired session.
    /// </summary>
    public async Task<string> OnUnauthorized()
    {
        try
        {
            _logger.LogWarning("Chat UI session expired — refreshing Trimble Identity token");
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var fromRequest = ResolveTrimbleAccessTokenFromRequest(httpContext);
                if (!string.IsNullOrEmpty(fromRequest))
                    return fromRequest;
            }

            _trimbleTokenService.InvalidateCache();
            var refreshed = await _trimbleTokenService.GetChatAccessTokenAsync();
            if (!string.IsNullOrEmpty(refreshed))
                return refreshed;

            return null!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling unauthorized access");
            throw;
        }
    }

    private string ResolveDisplayName()
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var userName = httpContext?.User?.FindFirst(ClaimTypes.Name)?.Value;
            var displayName = httpContext?.User?.FindFirst("displayName")?.Value;
            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName;
            if (!string.IsNullOrWhiteSpace(userName))
                return userName;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not resolve display name from HTTP context");
        }

        return "Administrator";
    }

    /// <summary>
    /// Optional extra run-context text. Default is empty so Agent Studio system prompt is the single source of truth
    /// (embed + Studio behave the same). Set AgentRunContextGuide in appsettings only if you need embed-only hints.
    /// </summary>
    private string BuildAgentRunContextGuide(string displayName)
    {
        if (!string.IsNullOrWhiteSpace(_config.AgentRunContextGuide))
            return _config.AgentRunContextGuide;

        return string.Empty;
    }

    private string ResolveChatTokenSource(HttpContext? httpContext)
    {
        if (httpContext != null && !string.IsNullOrWhiteSpace(httpContext.Request.Headers["X-Trimble-Access-Token"].FirstOrDefault()))
            return "request_header";
        if (!string.IsNullOrWhiteSpace(_config.TrimbleAccessToken))
            return "appsettings";
        return "client_credentials";
    }

    private Dictionary<string, RuntimeToolConfig> CreateLocalRuntimeTools() =>
        new()
        {
            ["get_current_user"] = new RuntimeToolConfig
            {
                Definition = new Tool
                {
                    Name = "get_current_user",
                    Description = "Returns the current authenticated user information"
                },
                Callback = InvokeGetCurrentUserAsync,
                TimeOutInMs = 5000
            }
            // get_server_status removed: stub often left the embed UI stuck on "running".
            // Use dbadash tool (op=exec) in Agent Studio for instance/server status.
        };

    private Task<string> InvokeGetCurrentUserAsync(Dictionary<string, JToken> _)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var userName = httpContext?.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
            var displayName = httpContext?.User?.FindFirst("displayName")?.Value ?? userName;
            var userInfo = new
            {
                username = userName,
                displayName,
                timestamp = DateTime.UtcNow
            };
            _logger.LogDebug("get_current_user invoked for {User}", displayName);
            return Task.FromResult(JsonSerializer.Serialize(userInfo));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "get_current_user failed; returning safe fallback");
            return Task.FromResult(JsonSerializer.Serialize(new
            {
                username = "Administrator",
                displayName = "Administrator",
                timestamp = DateTime.UtcNow
            }));
        }
    }

}
