namespace DBADashWebView.AgenticChat;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Obtains Trimble Identity access tokens via OAuth2 client credentials
/// (client_id + client_secret from Trimble Cloud Console).
/// </summary>
public interface ITrimbleIdentityTokenService
{
    string? ActiveChatScope { get; }
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
    Task<string?> GetChatAccessTokenAsync(CancellationToken cancellationToken = default);
    void InvalidateCache();
}

public sealed class TrimbleIdentityTokenService : ITrimbleIdentityTokenService
{
    private readonly HttpClient _httpClient;
    private readonly AgenticChatConfiguration _config;
    private readonly ILogger<TrimbleIdentityTokenService> _logger;
    private readonly TokenCache _workflowCache = new();
    private readonly TokenCache _chatCache = new();
    private string? _activeChatScope;

    public string? ActiveChatScope => _activeChatScope;

    public TrimbleIdentityTokenService(
        HttpClient httpClient,
        AgenticChatConfiguration config,
        ILogger<TrimbleIdentityTokenService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public void InvalidateCache()
    {
        _workflowCache.Clear();
        _chatCache.Clear();
    }

    public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
        _workflowCache.GetAsync(
            () => RequestTokenAsync(_config.TrimbleScope, ResolveWorkflowsAudience(), cancellationToken),
            IsClientCredentialsConfigured,
            cancellationToken);

    public Task<string?> GetChatAccessTokenAsync(CancellationToken cancellationToken = default) =>
        _chatCache.GetAsync(
            () => ResolveChatTokenWithProbeAsync(cancellationToken),
            IsClientCredentialsConfigured,
            cancellationToken);

    private async Task<(string Token, DateTimeOffset ExpiresAt)?> ResolveChatTokenWithProbeAsync(
        CancellationToken cancellationToken)
    {
        var primaryScope = ResolveChatScope() ?? "agents";

        if (_config.SkipAgentsApiProbe)
        {
            var fast = await RequestTokenAsync(primaryScope, null, cancellationToken);
            if (fast != null)
            {
                _activeChatScope = primaryScope;
                _logger.LogInformation(
                    "Trimble chat token acquired (scope={Scope}, probe skipped)",
                    primaryScope);
                return fast;
            }
        }

        var audienceFallback = ResolveChatAudience();
        var tried = 0;
        const int maxAttempts = 6;

        foreach (var scope in GetChatScopeCandidates())
        {
            foreach (var audience in new[] { (string?)null, audienceFallback })
            {
                if (++tried > maxAttempts)
                {
                    _logger.LogWarning(
                        "Stopping chat token probe after {Max} attempts; use SkipAgentsApiProbe or narrow TrimbleChatScopeCandidates",
                        maxAttempts);
                    break;
                }

                var token = await RequestTokenAsync(scope, audience, cancellationToken);
                if (token == null)
                    continue;

                if (_config.SkipAgentsApiProbe)
                {
                    _activeChatScope = scope;
                    return token;
                }

                var probe = await TrimbleAgentsApiProbe.ProbeAsync(
                    _httpClient, _config, token.Value.Token, cancellationToken);
                if (probe.Ok)
                {
                    _activeChatScope = scope;
                    _logger.LogInformation(
                        "Trimble chat token OK for scope {Scope} (audience={Audience})",
                        scope,
                        audience ?? "(none)");
                    return token;
                }

                _logger.LogWarning(
                    "Trimble scope {Scope} (audience={Audience}) rejected by agents API ({Status}): {Detail}",
                    scope,
                    audience ?? "(none)",
                    probe.StatusCode,
                    probe.Message);
            }

            if (tried > maxAttempts)
                break;
        }

        _logger.LogWarning(
            "No scope passed agents.stage probe; using {Scope} without audience",
            primaryScope);
        return await RequestTokenAsync(primaryScope, null, cancellationToken);
    }

    private IEnumerable<string> GetChatScopeCandidates()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var list = new List<string>();
        if (_config.TrimbleChatScopeCandidates is { Length: > 0 })
        {
            foreach (var s in _config.TrimbleChatScopeCandidates)
            {
                if (!string.IsNullOrWhiteSpace(s) && seen.Add(s.Trim()))
                    list.Add(s.Trim());
            }
        }

        foreach (var s in new[]
                 {
                     _config.TrimbleChatScope,
                     "agents",
                     "DBA_Dash_Web_View agents",
                     "agentic agents",
                     _config.TrimbleScope,
                     "agentic",
                     "DBA_Dash_Web_View"
                 })
        {
            if (!string.IsNullOrWhiteSpace(s) && seen.Add(s.Trim()))
                list.Add(s.Trim());
        }

        return list;
    }

    private sealed class TokenCache
    {
        private readonly SemaphoreSlim _gate = new(1, 1);
        private string? _token;
        private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

        public void Clear()
        {
            _token = null;
            _expiresAt = DateTimeOffset.MinValue;
        }

        public async Task<string?> GetAsync(
            Func<Task<(string Token, DateTimeOffset ExpiresAt)?>> fetch,
            Func<bool> canFetch,
            CancellationToken cancellationToken)
        {
            if (!canFetch())
                return null;

            if (_token != null && DateTimeOffset.UtcNow < _expiresAt)
                return _token;

            await _gate.WaitAsync(cancellationToken);
            try
            {
                if (_token != null && DateTimeOffset.UtcNow < _expiresAt)
                    return _token;

                var result = await fetch();
                if (result == null)
                    return null;

                _token = result.Value.Token;
                _expiresAt = result.Value.ExpiresAt;
                return _token;
            }
            finally
            {
                _gate.Release();
            }
        }
    }

    private string? ResolveChatScope() =>
        string.IsNullOrWhiteSpace(_config.TrimbleChatScope)
            ? _config.TrimbleScope
            : _config.TrimbleChatScope;

    private string ResolveWorkflowsAudience()
    {
        var configured = ResolveAudience(_config.TrimbleAudience);
        if (configured != null)
            return configured;

        return _config.Environment.Equals("Prod", StringComparison.OrdinalIgnoreCase)
            ? "https://cloud.api.trimblecloud.com"
            : _config.Environment.Equals("Dev", StringComparison.OrdinalIgnoreCase)
                ? "https://cloud.dev.api.trimblecloud.com"
                : "https://cloud.stage.api.trimblecloud.com";
    }

    private string ResolveChatAudience() =>
        ResolveAudience(_config.TrimbleChatAudience) ?? ResolveAgentsApiOrigin();

    private string? ResolveAgentsApiOrigin()
    {
        if (string.IsNullOrWhiteSpace(_config.TrimbleAgentsApiBaseUrl))
            return _config.Environment.Equals("Prod", StringComparison.OrdinalIgnoreCase)
                ? "https://agents.trimble-ai.com"
                : "https://agents.stage.trimble-ai.com";

        if (Uri.TryCreate(_config.TrimbleAgentsApiBaseUrl, UriKind.Absolute, out var uri))
            return $"{uri.Scheme}://{uri.Host}";

        return _config.TrimbleAgentsApiBaseUrl.TrimEnd('/');
    }

    private bool IsClientCredentialsConfigured() =>
        !string.IsNullOrWhiteSpace(_config.TrimbleClientId) &&
        !string.IsNullOrWhiteSpace(_config.TrimbleClientSecret);

    private async Task<(string Token, DateTimeOffset ExpiresAt)?> RequestTokenAsync(
        string? scope,
        string? audience,
        CancellationToken cancellationToken)
    {
        var tokenUrl = string.IsNullOrWhiteSpace(_config.TrimbleTokenUrl)
            ? "https://id.trimble.com/oauth/token"
            : _config.TrimbleTokenUrl;

        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_config.TrimbleClientId}:{_config.TrimbleClientSecret}"));

        using var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var form = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "client_credentials")
        };
        if (!string.IsNullOrWhiteSpace(scope))
            form.Add(new KeyValuePair<string, string>("scope", scope));
        if (!string.IsNullOrWhiteSpace(audience))
            form.Add(new KeyValuePair<string, string>("audience", audience));

        request.Content = new FormUrlEncodedContent(form);

        _logger.LogInformation(
            "Requesting Trimble token from {TokenUrl} (scope={Scope}, audience={Audience})",
            tokenUrl,
            scope ?? "(none)",
            audience ?? "(none)");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Trimble token request failed ({StatusCode}, scope={Scope}): {Body}",
                (int)response.StatusCode,
                scope,
                body);
            return null;
        }

        var tokenResponse = JsonSerializer.Deserialize<TrimbleTokenResponse>(body);
        if (string.IsNullOrWhiteSpace(tokenResponse?.AccessToken))
        {
            _logger.LogError("Trimble token response did not include access_token");
            return null;
        }

        var expiresIn = tokenResponse.ExpiresIn > 0 ? tokenResponse.ExpiresIn : 3600;
        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, expiresIn - 60));

        _logger.LogInformation(
            "Trimble Identity access token acquired (scope={Scope}, expires in {Seconds}s)",
            scope,
            expiresIn);
        MaybeWriteTokenDebugFile(tokenResponse.AccessToken);
        return (tokenResponse.AccessToken, expiresAt);
    }

    private void MaybeWriteTokenDebugFile(string accessToken)
    {
        if (!_config.DebugWriteTokenToFile)
            return;

        try
        {
            var path = string.IsNullOrWhiteSpace(_config.DebugTokenFilePath)
                ? Path.Combine(AppContext.BaseDirectory, "logs", "trimble_token_debug.txt")
                : _config.DebugTokenFilePath!;

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(path, accessToken);
            _logger.LogWarning(
                "DEBUG: Trimble token written to {Path} — set AgenticChat:DebugWriteTokenToFile to false when done",
                path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DEBUG: could not write Trimble token debug file");
        }
    }

    private static string? ResolveAudience(string? configured)
    {
        if (!string.IsNullOrWhiteSpace(configured))
            return configured.TrimEnd('/');

        return null;
    }

    private sealed class TrimbleTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
    }
}
