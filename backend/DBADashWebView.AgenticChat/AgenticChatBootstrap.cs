using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Trimble.AgenticChat.Core.AgenticModels;
using Trimble.AgenticChat.Core.Interfaces;
using TrimbleAgenticChat = Trimble.AgenticChat.Core.AgenticChat;

namespace DBADashWebView.AgenticChat;

public static class AgenticChatBootstrap
{
    public static void ConfigureServices(WebApplicationBuilder builder) =>
        ConfigureServices(builder.Services, builder.Configuration);

    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var agenticConfig = configuration.GetSection("AgenticChat").Get<AgenticChatConfiguration>()
            ?? new AgenticChatConfiguration();

        services.AddSingleton(agenticConfig);
        services.AddHttpClient();
        services.AddHttpClient<ITrimbleIdentityTokenService, TrimbleIdentityTokenService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(20);
        });
        services.AddHttpClient<ITrimbleAgentAccessService, TrimbleAgentAccessService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(20);
        });
        if (agenticConfig.Enabled &&
            !string.IsNullOrWhiteSpace(agenticConfig.TrimbleClientId) &&
            !string.IsNullOrWhiteSpace(agenticConfig.TrimbleClientSecret))
        {
            services.AddHostedService<TrimbleChatTokenWarmupHostedService>();
        }

        if (agenticConfig.RegisterApplicationWithAgent)
            services.AddHostedService<TrimbleAgentRegistrationHostedService>();
        services.AddHttpContextAccessor();
        services.AddScoped<IAgenticChatProvider, DbaAgenticChatProvider>();
        services.AddScoped<IAgenticChatEventHandler, DbaAgenticChatEventHandler>();
        services.AddScoped<IAgenticChatExceptionHandler, DbaAgenticChatExceptionHandler>();
        services.AddScoped<IRawAgenticChatMessageHandler, DbaRawAgenticChatMessageHandler>();
    }

    /// <summary>Called from AgenticChatLoader when plugin runs in an isolated load context (macOS).</summary>
    public static void MapEndpointsForHost(object hostApplication) =>
        typeof(AgenticChatBootstrap).GetMethod(nameof(MapEndpoints), new[] { typeof(WebApplication) })!
            .Invoke(null, new[] { hostApplication });

    public static void MapEndpoints(WebApplication app)
    {
        app.MapGet("/api/chat/diagnostics", async (
            AgenticChatConfiguration cfg,
            ITrimbleIdentityTokenService tokenService,
            ITrimbleAgentAccessService agentAccess,
            IHttpClientFactory httpClientFactory) =>
        {
            var issues = new List<string>();
            if (string.IsNullOrWhiteSpace(cfg.AgentId))
                issues.Add("AgentId is empty in appsettings.json");
            if (string.IsNullOrWhiteSpace(cfg.TrimbleClientId) || string.IsNullOrWhiteSpace(cfg.TrimbleClientSecret))
                issues.Add("TrimbleClientId or TrimbleClientSecret is not configured");

            var chatScope = string.IsNullOrWhiteSpace(cfg.TrimbleChatScope) ? cfg.TrimbleScope : cfg.TrimbleChatScope;
            var chatToken = await tokenService.GetChatAccessTokenAsync();
            if (string.IsNullOrEmpty(chatToken))
                issues.Add("Could not obtain Trimble chat token (check client id/secret, TrimbleChatScope, and token URL)");

            string? tokenAudience = null;
            string? tokenScopeClaim = null;
            if (!string.IsNullOrEmpty(chatToken))
            {
                tokenAudience = TryReadJwtClaim(chatToken, "aud");
                tokenScopeClaim = TryReadJwtClaim(chatToken, "scope");
            }

            AgentsApiProbeResult? agentsProbe = null;
            if (!string.IsNullOrEmpty(chatToken))
            {
                var http = httpClientFactory.CreateClient();
                agentsProbe = await TrimbleAgentsApiProbe.ProbeAsync(http, cfg, chatToken);
                if (!agentsProbe.Ok && !string.IsNullOrEmpty(agentsProbe.Message))
                    issues.Add($"agents.stage: {agentsProbe.Message}");
            }

            if (cfg.RegisterApplicationWithAgent && !string.IsNullOrEmpty(chatToken))
            {
                try
                {
                    await agentAccess.RegisterApplicationAccessAsync();
                }
                catch (Exception ex)
                {
                    issues.Add($"Agent registration failed: {ex.Message}");
                }
            }
            else if (cfg.RegisterApplicationWithAgent && string.IsNullOrEmpty(chatToken))
            {
                issues.Add("RegisterApplicationWithAgent is true but no Trimble chat token is available");
            }

            var tokenSource = !string.IsNullOrEmpty(chatToken)
                ? (string.IsNullOrWhiteSpace(cfg.TrimbleAccessToken) ? "client_credentials" : "trimble_access_token_config")
                : "none";

            return Results.Ok(new
            {
                ok = issues.Count == 0,
                agentId = cfg.AgentId,
                environment = cfg.Environment,
                trimbleChatTokenAcquired = !string.IsNullOrEmpty(chatToken),
                trimbleScope = cfg.TrimbleScope,
                trimbleChatScope = chatScope,
                activeChatScope = tokenService.ActiveChatScope,
                trimbleChatAudience = cfg.TrimbleChatAudience,
                trimbleAudienceRequested = cfg.TrimbleAudience,
                tokenAudienceClaim = tokenAudience,
                tokenScopeClaim,
                agentsApiOk = agentsProbe?.Ok,
                agentsApiStatusCode = agentsProbe?.StatusCode,
                agentsApiDetail = agentsProbe?.Detail,
                registerApplicationWithAgent = cfg.RegisterApplicationWithAgent,
                trimbleAgentsApiBaseUrl = cfg.TrimbleAgentsApiBaseUrl,
                trimbleWorkflowsApiBaseUrl = cfg.TrimbleWorkflowsApiBaseUrl,
                trimbleApplicationId = cfg.TrimbleApplicationId,
                skipAgentsApiProbe = cfg.SkipAgentsApiProbe,
                tokenSource,
                agentStudioParityMode = cfg.AgentStudioParityMode,
                embedNote =
                    "Agent Studio parity: set AgentStudioParityMode=true (default), publish the same agent in Studio, "
                    + "and use a Trimble USER token (TrimbleAccessToken or ?trimbleToken=) — not only client_credentials.",
                issues
            });
        }).RequireAuthorization();

        app.MapGet("/api/chat/health", () => Results.Ok(new
        {
            status = "healthy",
            enabled = true,
            timestamp = DateTime.UtcNow,
            service = "AgenticChat"
        }));

        app.MapGet("/api/chat/config", async (IAgenticChatProvider provider) =>
        {
            var config = await provider.ProvideChatUiConfig();
            return Results.Json(config);
        }).RequireAuthorization();

        app.MapPost("/api/chat/message", async (
            HttpContext httpContext,
            IAgenticChatProvider provider,
            IAgenticChatEventHandler? eventHandler,
            IAgenticChatExceptionHandler? exceptionHandler,
            ILoggerFactory loggerFactory) =>
        {
            using var reader = new StreamReader(httpContext.Request.Body);
            var messageJson = await reader.ReadToEndAsync();
            var logger = loggerFactory.CreateLogger("AgenticChat");
            var agenticChat = new TrimbleAgenticChat(provider, eventHandler);

            try
            {
                var response = await agenticChat.HandleReceivedMessageAsync(messageJson);
                if (response == null)
                    return Results.Json(new { status = "no_response" });

                var responseJson = agenticChat.SerializeResponseMessage(response);
                return Results.Content(responseJson, "application/json");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error handling chat message");
                exceptionHandler?.OnException(ex);

                var errorResponse = agenticChat.TryParseExceptionResponse(messageJson, ex);
                if (errorResponse != null)
                {
                    var errorJson = agenticChat.SerializeResponseMessage(errorResponse);
                    return Results.Content(errorJson, "application/json");
                }

                return Results.BadRequest(new { error = "Failed to process chat message" });
            }
        }).RequireAuthorization();

        app.MapPost("/api/chat/config/update", async (
            HttpContext httpContext,
            IAgenticChatProvider provider,
            IAgenticChatEventHandler? eventHandler) =>
        {
            using var reader = new StreamReader(httpContext.Request.Body);
            var body = await reader.ReadToEndAsync();
            var agenticChat = new TrimbleAgenticChat(provider, eventHandler);
            var newConfig = JsonConvert.DeserializeObject<ChatUiConfiguration>(body)
                ?? throw new InvalidOperationException("Invalid config");
            var response = agenticChat.GetConfigUpdateResponse(newConfig);
            var responseJson = agenticChat.SerializeResponseMessage(response);
            return Results.Content(responseJson, "application/json");
        }).RequireAuthorization();
    }

    private static string? TryReadJwtClaim(string jwt, string claimName)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length < 2) return null;
            var payload = parts[1];
            var pad = payload.Length % 4;
            if (pad > 0) payload += new string('=', 4 - pad);
            var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload.Replace('-', '+').Replace('_', '/')));
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty(claimName, out var claim))
                return null;

            if (claim.ValueKind == System.Text.Json.JsonValueKind.String)
                return claim.GetString();
            if (claim.ValueKind == System.Text.Json.JsonValueKind.Array && claim.GetArrayLength() > 0)
                return claim[0].GetString();
            return claim.ToString();
        }
        catch
        {
            return null;
        }
    }
}
