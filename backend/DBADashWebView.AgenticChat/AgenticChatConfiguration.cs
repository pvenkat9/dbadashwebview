namespace DBADashWebView.AgenticChat;

/// <summary>
/// Configuration for Trimble Agentic Chat UI
/// </summary>
public class AgenticChatConfiguration
{
    /// <summary>When false, chat is not loaded (dashboard still starts).</summary>
    public bool Enabled { get; set; } = true;

    public string Environment { get; set; } = "Stage"; // Stage | Prod
    public string AgentId { get; set; } = "";
    public string Theme { get; set; } = "Light"; // Light | Dark
    public string Variant { get; set; } = "Full"; // Full | Minimal | Narrow
    public string ContentVariant { get; set; } = "Chat"; // Chat | AgentCards
    public string Locale { get; set; } = "en";
    public bool HideModelSelection { get; set; } = false;
    public int OnBeforeRunTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// When true, skip agents.stage probe after OAuth and use the first successful chat-scope token.
    /// Prevents "token request timed out" when many scope/audience combinations are tried.
    /// </summary>
    public bool SkipAgentsApiProbe { get; set; } = true;

    /// <summary>
    /// When true (default), embed matches Agent Studio: no extra run context, no local tools.
    /// </summary>
    public bool AgentStudioParityMode { get; set; } = true;

    /// <summary>
    /// Local tools (get_current_user). Ignored when <see cref="AgentStudioParityMode"/> is true.
    /// </summary>
    public bool RegisterLocalRuntimeTools { get; set; }

    /// <summary>
    /// Optional override for run-context guidance shown to the agent each turn.
    /// </summary>
    public string? AgentRunContextGuide { get; set; }

    /// <summary>
    /// Optional: paste a Trimble user access token from Assist (browser devtools) when client-credentials
    /// scopes cannot call agents.stage. Takes precedence over OAuth client credentials.
    /// </summary>
    public string? TrimbleAccessToken { get; set; }

    // ── Trimble Identity (client credentials from Trimble Cloud Console) ───────

    /// <summary>OAuth token endpoint. Default: https://id.trimble.com/oauth/token</summary>
    public string TrimbleTokenUrl { get; set; } = "https://id.trimble.com/oauth/token";

    public string? TrimbleClientId { get; set; }
    public string? TrimbleClientSecret { get; set; }

    /// <summary>OAuth scope for workflows / general APIs (e.g. Agentic-N8N-Webhook).</summary>
    public string? TrimbleScope { get; set; }

    /// <summary>
    /// OAuth scope for embedded chat + agents.stage API. Must include <c>agents</c> (see SectorOperations:
    /// <c>OperationsSelfServiceBot agents kb models evals</c>). N8N/webhook-only scopes return 401 on agents.stage.
    /// </summary>
    public string? TrimbleChatScope { get; set; }

    /// <summary>
    /// Scopes to try for chat (first that passes agents.stage probe wins). Set in Trimble Console.
    /// </summary>
    public string[]? TrimbleChatScopeCandidates { get; set; }

    /// <summary>
    /// OAuth audience for workflows. Stage: https://cloud.stage.api.trimblecloud.com
    /// </summary>
    public string? TrimbleAudience { get; set; }

    /// <summary>
    /// OAuth audience when requesting the chat token. Defaults to agents API origin.
    /// </summary>
    public string? TrimbleChatAudience { get; set; }

    // ── Trimble Agents API (PATCH agent users — Chat embed only) ─────────────

    /// <summary>e.g. https://agents.stage.trimble-ai.com (not the workflows URL).</summary>
    public string? TrimbleAgentsApiBaseUrl { get; set; }

    /// <summary>
    /// Agentic Workflows API from Trimble portal (e.g. .../agentic/workflows/v1). Not used for chat PATCH.
    /// </summary>
    public string? TrimbleWorkflowsApiBaseUrl { get; set; }

    /// <summary>Application UUID from Trimble Cloud Console (used as applications:{app_id}).</summary>
    public string? TrimbleApplicationId { get; set; }

    /// <summary>
    /// On startup, PATCH /v2/agents/{AgentId} to grant applications:{TrimbleApplicationId}.
    /// </summary>
    public bool RegisterApplicationWithAgent { get; set; }

    /// <summary>
    /// DEBUG ONLY: write access_token to logs/trimble_token_debug.txt for jwt.io inspection. Turn off after debugging.
    /// </summary>
    public bool DebugWriteTokenToFile { get; set; }

    /// <summary>Optional override path (default: {site}/logs/trimble_token_debug.txt).</summary>
    public string? DebugTokenFilePath { get; set; }
}

/// <summary>
/// Chat UI URLs for different environments
/// </summary>
public static class AgenticChatConstants
{
    public static readonly Dictionary<string, string> ChatUiUrls = new()
    {
        { "Stage", "https://embed.stage.trimble-ai.com" },
        { "Prod", "https://embed.ai.trimble.com" },
        { "Dev", "https://embed.dev.trimble-ai.com" }
    };
}
