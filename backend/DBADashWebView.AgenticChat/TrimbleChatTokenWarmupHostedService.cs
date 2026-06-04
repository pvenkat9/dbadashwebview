namespace DBADashWebView.AgenticChat;

/// <summary>
/// Warms the Trimble chat token cache at startup so the embed's first token request is fast.
/// </summary>
public sealed class TrimbleChatTokenWarmupHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly AgenticChatConfiguration _config;
    private readonly ILogger<TrimbleChatTokenWarmupHostedService> _logger;

    public TrimbleChatTokenWarmupHostedService(
        IServiceProvider services,
        AgenticChatConfiguration config,
        ILogger<TrimbleChatTokenWarmupHostedService> logger)
    {
        _services = services;
        _config = config;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_config.Enabled)
            return Task.CompletedTask;

        if (string.IsNullOrWhiteSpace(_config.TrimbleClientId) ||
            string.IsNullOrWhiteSpace(_config.TrimbleClientSecret))
        {
            _logger.LogDebug("Chat token warmup skipped — Trimble client credentials not configured");
            return Task.CompletedTask;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _services.CreateScope();
                var tokenService = scope.ServiceProvider.GetRequiredService<ITrimbleIdentityTokenService>();
                var token = await tokenService.GetChatAccessTokenAsync(cancellationToken);
                if (!string.IsNullOrEmpty(token))
                    _logger.LogInformation("Trimble chat token warmed at startup (scope={Scope})", tokenService.ActiveChatScope);
                else
                    _logger.LogWarning("Trimble chat token warmup returned no token");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Trimble chat token warmup failed (first chat open may be slower)");
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
