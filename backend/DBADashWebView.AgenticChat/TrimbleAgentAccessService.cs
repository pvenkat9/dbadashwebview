namespace DBADashWebView.AgenticChat;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

/// <summary>
/// Registers the Trimble Cloud application with an agent via the Agents API:
/// PATCH /v2/agents/{agentId} with users: ["applications:{appId}"]
/// </summary>
public interface ITrimbleAgentAccessService
{
    Task RegisterApplicationAccessAsync(CancellationToken cancellationToken = default);
}

public sealed class TrimbleAgentAccessService : ITrimbleAgentAccessService
{
    private readonly HttpClient _httpClient;
    private readonly AgenticChatConfiguration _config;
    private readonly ITrimbleIdentityTokenService _tokenService;
    private readonly ILogger<TrimbleAgentAccessService> _logger;

    public TrimbleAgentAccessService(
        HttpClient httpClient,
        AgenticChatConfiguration config,
        ITrimbleIdentityTokenService tokenService,
        ILogger<TrimbleAgentAccessService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task RegisterApplicationAccessAsync(CancellationToken cancellationToken = default)
    {
        if (!_config.RegisterApplicationWithAgent)
        {
            _logger.LogDebug("Agent application registration is disabled");
            return;
        }

        if (string.IsNullOrWhiteSpace(_config.AgentId) || string.IsNullOrWhiteSpace(_config.TrimbleApplicationId))
        {
            _logger.LogWarning(
                "Cannot register agent access: AgentId or TrimbleApplicationId is not configured");
            return;
        }

        var baseUrl = ResolveAgentsApiBaseUrl();
        var token = await _tokenService.GetChatAccessTokenAsync(cancellationToken);
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Cannot register agent access: no Trimble access token");
            return;
        }

        var url = $"{baseUrl.TrimEnd('/')}/v2/agents/{_config.AgentId}";
        var payload = new
        {
            users = new[] { $"applications:{_config.TrimbleApplicationId}" }
        };

        using var request = new HttpRequestMessage(HttpMethod.Patch, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        _logger.LogInformation(
            "Registering application {AppId} with agent {AgentId} at {Url}",
            _config.TrimbleApplicationId,
            _config.AgentId,
            url);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Agent registration failed ({StatusCode}): {Body}",
                (int)response.StatusCode,
                body);
            return;
        }

        _logger.LogInformation("Application registered with agent successfully");
    }

    private string ResolveAgentsApiBaseUrl()
    {
        if (!string.IsNullOrWhiteSpace(_config.TrimbleAgentsApiBaseUrl))
            return _config.TrimbleAgentsApiBaseUrl;

        return _config.Environment.Equals("Prod", StringComparison.OrdinalIgnoreCase)
            ? "https://agents.trimble-ai.com"
            : "https://agents.stage.trimble-ai.com";
    }
}

/// <summary>
/// Optionally runs agent application registration once at startup.
/// </summary>
public sealed class TrimbleAgentRegistrationHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly AgenticChatConfiguration _config;
    private readonly ILogger<TrimbleAgentRegistrationHostedService> _logger;

    public TrimbleAgentRegistrationHostedService(
        IServiceProvider services,
        AgenticChatConfiguration config,
        ILogger<TrimbleAgentRegistrationHostedService> logger)
    {
        _services = services;
        _config = config;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_config.RegisterApplicationWithAgent)
            return;

        try
        {
            using var scope = _services.CreateScope();
            var access = scope.ServiceProvider.GetRequiredService<ITrimbleAgentAccessService>();
            await access.RegisterApplicationAccessAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Startup agent application registration failed");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
