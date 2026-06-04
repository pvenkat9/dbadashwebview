using Trimble.AgenticChat.Core;
using Trimble.AgenticChat.Core.AgenticModels;
using Trimble.AgenticChat.Core.Interfaces;
using Trimble.AgenticChat.Core.Models;
using Core = Trimble.AgenticChat.Core;

// Local check: run after `dotnet run` from backend/ChatProtocolVerify
// Fails if API would return JSON the Trimble embed cannot parse.

var chat = new AgenticChat(new StubProvider(), null);

var configReq = """{"type":"config","requestId":"test-1"}""";
var configRes = chat.HandleReceivedMessageAsync(configReq).GetAwaiter().GetResult()
    ?? throw new Exception("config returned null");
var configJson = chat.SerializeResponseMessage(configRes);

var tokenReq = """{"type":"token","requestId":"test-2"}""";
var tokenRes = chat.HandleReceivedMessageAsync(tokenReq).GetAwaiter().GetResult()
    ?? throw new Exception("token returned null");
var tokenJson = chat.SerializeResponseMessage(tokenRes);

var errors = new List<string>();
if (!configJson.Contains("\"type\":\"config\"") && !configJson.Contains("\"type\": \"config\""))
    errors.Add($"config response missing lowercase type: {configJson[..Math.Min(120, configJson.Length)]}");
if (configJson.Contains("\"Type\""))
    errors.Add("config response uses PascalCase Type (embed will show black screen)");
if (!tokenJson.Contains("\"type\":\"token\"") && !tokenJson.Contains("\"type\": \"token\""))
    errors.Add($"token response missing lowercase type: {tokenJson[..Math.Min(120, tokenJson.Length)]}");
if (!tokenJson.Contains("\"token\""))
    errors.Add("token response missing token field");

if (errors.Count > 0)
{
    Console.Error.WriteLine("CHAT PROTOCOL VERIFY FAILED:");
    foreach (var e in errors) Console.Error.WriteLine("  - " + e);
    System.Environment.Exit(1);
}

Console.WriteLine("OK: Trimble protocol serialization looks correct for embed.");
Console.WriteLine("config: " + configJson[..Math.Min(200, configJson.Length)] + "...");
Console.WriteLine("token:  " + tokenJson[..Math.Min(120, tokenJson.Length)] + "...");

sealed class StubProvider : IAgenticChatProvider
{
    public Task<ChatUiConfiguration> ProvideChatUiConfig() =>
        Task.FromResult(new ChatUiConfiguration
        {
            Environment = Core.Environment.Stage,
            AgentId = "00000000-0000-0000-0000-000000000001",
            UiConfig = new UiConfig { Theme = Theme.Light, Variant = ChatUiVariants.Full, ContentVariant = ContentVariants.Chat }
        });

    public Task<string> ProvideChatUiToken() => Task.FromResult("stub-trimble-token");

    public Task<OnBeforeRunConfig> ProvideRunConfig(string agentId) =>
        Task.FromResult(new OnBeforeRunConfig());

    public Task<string> OnUnauthorized() => Task.FromResult(string.Empty);
}
