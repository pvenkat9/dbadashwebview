namespace DBADashWebView.AgenticChat;

using System.Net.Http.Headers;
using System.Text.Json;

/// <summary>
/// Probes agents.stage to verify the chat token can load an agent (embed prerequisite).
/// </summary>
public static class TrimbleAgentsApiProbe
{
    public static async Task<AgentsApiProbeResult> ProbeAsync(
        HttpClient httpClient,
        AgenticChatConfiguration config,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(config.AgentId))
        {
            return new AgentsApiProbeResult(
                false,
                0,
                null,
                "AgentId is not configured");
        }

        var baseUrl = string.IsNullOrWhiteSpace(config.TrimbleAgentsApiBaseUrl)
            ? "https://agents.stage.trimble-ai.com"
            : config.TrimbleAgentsApiBaseUrl.TrimEnd('/');

        // Do not use fields=tools — agents v2 rejects it (422) and slows token acquisition.
        var url = $"{baseUrl}/v2/agents/{config.AgentId}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return new AgentsApiProbeResult(true, (int)response.StatusCode, null, null);
        }

        var detail = TryReadDetail(body);
        var message = detail ?? $"HTTP {(int)response.StatusCode}";

        if (message.Contains("Invalid token audience", StringComparison.OrdinalIgnoreCase))
        {
            message +=
                " — client-credentials scopes (Agentic-N8N-Webhook, agentic, DBA_Dash_Web_View) "
                + "do not authorize agents.stage. Ask Trimble for an Agents API scope, or set "
                + "AgenticChat:TrimbleAccessToken to a user token from Assist (browser devtools).";
        }

        return new AgentsApiProbeResult(false, (int)response.StatusCode, detail, message);
    }

    private static string? TryReadDetail(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("detail", out var detail))
                return detail.GetString();
            if (doc.RootElement.TryGetProperty("title", out var title))
                return title.GetString();
        }
        catch
        {
            // ignore
        }

        return body.Length > 200 ? body[..200] : body;
    }
}

public sealed record AgentsApiProbeResult(
    bool Ok,
    int StatusCode,
    string? Detail,
    string? Message);
