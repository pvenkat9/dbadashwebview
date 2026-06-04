namespace DBADashWebView.AgenticChat;

using System.Security.Claims;
using Trimble.AgenticChat.Core.AgenticModels;
using Trimble.AgenticChat.Core.Interfaces;

/// <summary>
/// Handles Chat UI navigation events and user interactions
/// 
/// This handler tracks user interactions with the Chat UI and can:
/// - Log sessions to database for audit trails
/// - Track feature usage/analytics
/// - Send alerts for critical operations
/// - Update user activity status
/// </summary>
public class DbaAgenticChatEventHandler : IAgenticChatEventHandler
{
    private readonly ILogger<DbaAgenticChatEventHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DbaAgenticChatEventHandler(
        ILogger<DbaAgenticChatEventHandler> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Called when the user interacts with the Chat UI
    /// </summary>
    public void OnEvent(ChatUiEvent evt)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var userName = httpContext?.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
            var timestamp = DateTime.UtcNow;

            _logger.LogInformation(
                "Chat UI Event: User={UserName}, Type={EventType}, Timestamp={Timestamp}",
                userName, evt.Type, timestamp);

            // Handle specific event types
            switch (evt.Type)
            {
                case ChatUiMessageType.OnAgentSelect:
                    HandleAgentSelect(userName, evt.Payload, timestamp);
                    break;

                case ChatUiMessageType.OnThreadSelect:
                    HandleThreadSelect(userName, evt.Payload, timestamp);
                    break;

                case ChatUiMessageType.OnNewChat:
                    HandleNewChat(userName, timestamp);
                    break;

                case ChatUiMessageType.OnExploreAgents:
                    HandleExploreAgents(userName, timestamp);
                    break;

                case ChatUiMessageType.OnCreateAgent:
                    HandleCreateAgent(userName, evt.Payload, timestamp);
                    break;

                case ChatUiMessageType.OnMyTrimbleClick:
                    HandleMyTrimbleClick(userName, timestamp);
                    break;

                case ChatUiMessageType.OnChatInputButtonClick:
                    HandleChatInputButtonClick(userName, evt.Payload, timestamp);
                    break;

                case ChatUiMessageType.OnSignIn:
                    HandleSignIn(userName, timestamp);
                    break;

                case ChatUiMessageType.OnClose:
                    HandleChatClose(userName, timestamp);
                    break;

                default:
                    _logger.LogWarning("Unknown Chat UI event type: {EventType}", evt.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Chat UI event of type: {EventType}", evt?.Type);
        }
    }

    private void HandleAgentSelect(string userName, string? payload, DateTime timestamp)
    {
        _logger.LogInformation(
            "[AUDIT] User {UserName} selected agent. Payload: {Payload}, Time: {Timestamp}",
            userName, payload, timestamp);

        // TODO: Implement custom logic
        // Example use cases:
        // 1. Log to database for audit trail
        //    INSERT INTO ChatAuditLog (User, Action, Agent, Timestamp) VALUES (...)
        // 2. Track agent popularity
        //    UPDATE AgentStats SET SelectCount = SelectCount + 1 WHERE AgentId = ...
        // 3. Send notification if specific agent selected
        //    if (payload == "AdminTools") SendAlertToAdmins();
        // 4. Update user's recent agents
        //    cache.Set($"user:{userName}:recentAgent", payload);
    }

    private void HandleThreadSelect(string userName, string? payload, DateTime timestamp)
    {
        _logger.LogInformation(
            "[AUDIT] User {UserName} selected thread {ThreadId}. Time: {Timestamp}",
            userName, payload, timestamp);

        // TODO: Implement custom logic
        // Example use cases:
        // 1. Resume conversation tracking
        //    LogThreadResume(userName, payload, timestamp);
        // 2. Load previous context
        //    context = GetThreadContext(payload);
        // 3. Track conversation branches
        //    IncrementThreadMetric(payload, "resumed");
    }

    private void HandleNewChat(string userName, DateTime timestamp)
    {
        _logger.LogInformation(
            "[SESSION] New chat session started for user {UserName} at {Timestamp}",
            userName, timestamp);

        // TODO: Implement custom logic
        // Example use cases:
        // 1. Create chat session record
        //    var sessionId = CreateChatSession(userName, timestamp);
        // 2. Initialize session metrics
        //    StartSessionMetrics(sessionId);
        // 3. Log to database
        //    INSERT INTO ChatSessions (UserId, StartTime, Status) VALUES (...)
        // 4. Clear previous context
        //    ClearUserContext(userName);
    }

    private void HandleExploreAgents(string userName, DateTime timestamp)
    {
        _logger.LogInformation(
            "[ACTION] User {UserName} exploring available agents at {Timestamp}",
            userName, timestamp);

        // TODO: Implement custom logic
        // Example use cases:
        // 1. Track exploration behavior for analytics
        //    RecordUserAction(userName, "explore_agents", timestamp);
        // 2. Log for UX analysis
        //    AnalyticsService.Track("agent_exploration", userName);
        // 3. Suggest new agents
        //    SuggestNewAgentsBasedOnUsage(userName);
    }

    private void HandleCreateAgent(string userName, string? payload, DateTime timestamp)
    {
        _logger.LogWarning(
            "[CRITICAL] User {UserName} attempting to create new agent. Payload: {Payload}, Time: {Timestamp}",
            userName, payload, timestamp);

        // TODO: Implement custom logic
        // Example use cases:
        // 1. Check permissions before allowing
        //    if (!HasPermission(userName, "create_agent"))
        //        SendAlert($"Unauthorized agent creation attempt by {userName}");
        // 2. Log for compliance
        //    INSERT INTO AgentCreationAudit (User, AttemptTime) VALUES (...)
        // 3. Send notification to admins
        //    SendAdminAlert($"User {userName} created agent: {payload}");
        // 4. Track new agent creation
        //    LogNewAgentCreation(userName, payload);
    }

    private void HandleMyTrimbleClick(string userName, DateTime timestamp)
    {
        _logger.LogInformation(
            "[ACTION] User {UserName} clicked My Trimble link at {Timestamp}",
            userName, timestamp);

        // TODO: Implement custom logic
        // Example use cases:
        // 1. Track external navigation
        //    RecordExternalNavigation(userName, "MyTrimble", timestamp);
        // 2. Log for support/UX tracking
        //    AnalyticsService.Track("my_trimble_click", userName);
    }

    private void HandleChatInputButtonClick(string userName, string? payload, DateTime timestamp)
    {
        _logger.LogDebug(
            "[INTERACTION] User {UserName} clicked chat button. Button: {Button}, Time: {Timestamp}",
            userName, payload, timestamp);

        // TODO: Implement custom logic
        // Example use cases:
        // 1. Track button clicks for UX improvement
        //    TrackButtonClick(payload, userName);
        // 2. Log special action buttons
        //    if (payload == "DeleteDatabase") LogCriticalOperation(userName, payload);
        // 3. Update interaction metrics
        //    IncrementButtonMetric(payload);
        // 4. Validate dangerous operations
        //    if (IsDangerousOperation(payload)) RequestConfirmation(userName);
    }

    private void HandleSignIn(string userName, DateTime timestamp)
    {
        _logger.LogInformation(
            "[SESSION] User {UserName} signed in to Chat UI at {Timestamp}",
            userName, timestamp);

        // TODO: Implement custom logic
        // Example use cases:
        // 1. Update user's last activity
        //    UpdateUserLastActivity(userName, timestamp);
        // 2. Log successful authentication
        //    INSERT INTO AuthAudit (User, Event, Time) VALUES (...)
        // 3. Send welcome/notification
        //    SendUserNotification(userName, "Welcome back!");
        // 4. Initialize user preferences
        //    LoadUserPreferences(userName);
    }

    private void HandleChatClose(string userName, DateTime timestamp)
    {
        _logger.LogInformation(
            "[SESSION] Chat UI closed for user {UserName} at {Timestamp}",
            userName, timestamp);

        // TODO: Implement custom logic
        // Example use cases:
        // 1. End chat session
        //    EndChatSession(userName, timestamp);
        // 2. Save conversation for future reference
        //    SaveChatTranscript(userName);
        // 3. Update session statistics
        //    UpdateSessionStats(userName, CalculateSessionDuration());
        // 4. Cleanup user context
        //    ClearUserContext(userName);
        // 5. Log for compliance
        //    INSERT INTO SessionLog (User, EndTime) VALUES (...)
    }
}

/// <summary>
/// Handles exceptions that occur during message handling in the Chat UI
/// 
/// This handler catches errors and can:
/// - Send to error tracking services (Sentry, App Insights, etc.)
/// - Alert administrators for critical errors
/// - Track error metrics and patterns
/// - Log errors for debugging
/// </summary>
public class DbaAgenticChatExceptionHandler : IAgenticChatExceptionHandler
{
    private readonly ILogger<DbaAgenticChatExceptionHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DbaAgenticChatExceptionHandler(
        ILogger<DbaAgenticChatExceptionHandler> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Called when an exception occurs during Chat UI message handling
    /// </summary>
    public void OnException(Exception exception)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var userName = httpContext?.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
            var timestamp = DateTime.UtcNow;

            _logger.LogError(
                exception,
                "[ERROR] Chat UI exception for user {UserName} at {Timestamp}. Exception: {ExceptionMessage}",
                userName, timestamp, exception.Message);

            // Categorize error severity
            var severity = CategorizeErrorSeverity(exception);

            // TODO: Implement error handling based on severity
            switch (severity)
            {
                case ErrorSeverity.Critical:
                    HandleCriticalError(userName, exception, timestamp);
                    break;

                case ErrorSeverity.High:
                    HandleHighSeverityError(userName, exception, timestamp);
                    break;

                case ErrorSeverity.Medium:
                    HandleMediumSeverityError(userName, exception, timestamp);
                    break;

                case ErrorSeverity.Low:
                    HandleLowSeverityError(userName, exception, timestamp);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error in exception handler itself");
        }
    }

    private ErrorSeverity CategorizeErrorSeverity(Exception ex)
    {
        return ex switch
        {
            // Critical: System/Infrastructure errors
            NullReferenceException => ErrorSeverity.Critical,
            OutOfMemoryException => ErrorSeverity.Critical,
            StackOverflowException => ErrorSeverity.Critical,

            // High: Authentication/Authorization errors
            UnauthorizedAccessException => ErrorSeverity.High,

            // High: Database/Connection errors
            Microsoft.Data.SqlClient.SqlException => ErrorSeverity.High,
            TimeoutException when ex.Message.Contains("database") => ErrorSeverity.High,

            // Medium: Validation/Input errors
            ArgumentException => ErrorSeverity.Medium,
            FormatException => ErrorSeverity.Medium,

            // Low: Other runtime errors
            _ => ErrorSeverity.Low
        };
    }

    private void HandleCriticalError(string userName, Exception exception, DateTime timestamp)
    {
        _logger.LogCritical(
            exception,
            "[CRITICAL] Critical error in Chat UI. User: {UserName}, Time: {Timestamp}",
            userName, timestamp);

        // TODO: Implement critical error handling
        // Example actions:
        // 1. Send alert to administrators immediately
        //    SendAdminAlert($"CRITICAL: Chat UI error for {userName}: {exception.Message}");
        // 2. Send to error tracking service
        //    SentryClient.CaptureException(exception, new { severity = "critical", userName });
        // 3. Log to critical incidents database
        //    INSERT INTO CriticalIncidents (User, Message, Timestamp) VALUES (...)
        // 4. Disable chat UI for this user
        //    DisableChatForUser(userName);
        // 5. Create support ticket
        //    CreateSupportTicket(userName, exception);
    }

    private void HandleHighSeverityError(string userName, Exception exception, DateTime timestamp)
    {
        _logger.LogError(
            exception,
            "[HIGH SEVERITY] Error in Chat UI. User: {UserName}, Time: {Timestamp}",
            userName, timestamp);

        // TODO: Implement high severity error handling
        // Example actions:
        // 1. Send to error tracking with warning level
        //    ErrorTracker.LogError(exception, "high", userName);
        // 2. Log to audit for compliance
        //    INSERT INTO ErrorLog (User, ErrorMessage, Severity) VALUES (...)
        // 3. Retry the operation if applicable
        //    if (IsRetryableError(exception)) RetryOperation();
        // 4. Notify user about issue
        //    SendUserNotification(userName, "We encountered an issue. Please try again.");
        // 5. Send summary report to admins
        //    if (ErrorCountExceeds(5)) SendErrorSummary();
    }

    private void HandleMediumSeverityError(string userName, Exception exception, DateTime timestamp)
    {
        _logger.LogWarning(
            exception,
            "[MEDIUM SEVERITY] Error in Chat UI. User: {UserName}, Time: {Timestamp}",
            userName, timestamp);

        // TODO: Implement medium severity error handling
        // Example actions:
        // 1. Log to monitoring system
        //    Metrics.IncrementCounter("chat_ui_errors", new { severity = "medium" });
        // 2. Add to error analytics
        //    AnalyticsService.LogError(exception, "medium", userName);
        // 3. Attempt recovery
        //    AttemptRecovery(exception);
    }

    private void HandleLowSeverityError(string userName, Exception exception, DateTime timestamp)
    {
        _logger.LogInformation(
            "[LOW SEVERITY] Error in Chat UI. User: {UserName}, Time: {Timestamp}",
            userName, timestamp);

        // TODO: Implement low severity error handling
        // Example actions:
        // 1. Log for troubleshooting
        //    DebugLog.Add(new { user = userName, error = exception.Message, time = timestamp });
        // 2. Track error metrics
        //    Metrics.IncrementCounter("chat_ui_warnings");
    }
}

/// <summary>
/// Optional handler for debugging: receives every raw JSON message
/// ⚠️ WARNING: Messages may contain sensitive info such as access tokens — do not persist them
/// 
/// This handler is useful for:
/// - Debugging message protocol issues
/// - Analyzing message patterns
/// - Troubleshooting communication failures
/// - Performance monitoring
/// </summary>
public class DbaRawAgenticChatMessageHandler : IRawAgenticChatMessageHandler
{
    private readonly ILogger<DbaRawAgenticChatMessageHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private static int _messageCount = 0;

    public DbaRawAgenticChatMessageHandler(
        ILogger<DbaRawAgenticChatMessageHandler> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Called for every raw JSON message (use for debugging only)
    /// </summary>
    public void OnMessage(RawMessageType messageType, string json)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var userName = httpContext?.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
            var direction = messageType.ToString().Contains("Request", StringComparison.OrdinalIgnoreCase)
                ? "→ INCOMING"
                : "← OUTGOING";
            var messageNum = Interlocked.Increment(ref _messageCount);

            // ⚠️ SECURITY: Only log message summary, NOT the full JSON (may contain tokens)
            var messageSummary = ExtractMessageSummary(json);

            _logger.LogDebug(
                "[MSG #{MessageNum}] {Direction} from {UserName}: {MessageSummary}",
                messageNum, direction, userName, messageSummary);

            // TODO: Implement debugging logic (only in development!)
            #if DEBUG
            // Example: Log full messages only in debug mode
            _logger.LogDebug("Full message: {FullJson}", json);
            
            // Example: Track message patterns
            TrackMessagePattern(messageSummary);
            
            // Example: Monitor for performance issues
            MonitorMessageSize(json);
            #endif
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error processing raw message in debug handler");
        }
    }

    private string ExtractMessageSummary(string json)
    {
        try
        {
            // Parse JSON to extract relevant info without exposing tokens
            if (json.Contains("\"type\""))
            {
                // Extract message type
                var typeMatch = System.Text.RegularExpressions.Regex.Match(json, "\"type\"\\s*:\\s*\"([^\"]+)\"");
                if (typeMatch.Success)
                {
                    var type = typeMatch.Groups[1].Value;
                    var length = json.Length;
                    return $"Type={type}, Length={length} bytes";
                }
            }

            return $"Length={json.Length} bytes";
        }
        catch
        {
            return "Unable to parse summary";
        }
    }

    private void TrackMessagePattern(string summary)
    {
        // TODO: Implement message pattern analysis
        // Example: Track message types to identify bottlenecks
        // if (summary.Contains("get_server_status"))
        //     Metrics.IncrementCounter("tool:get_server_status");
    }

    private void MonitorMessageSize(string json)
    {
        // TODO: Implement performance monitoring
        // Example: Alert if messages are too large
        // var size = json.Length;
        // if (size > 100_000) // 100KB
        //     _logger.LogWarning("Large message detected: {SizeInKB} KB", size / 1024);
        // 
        // Metrics.RecordGauge("message_size_bytes", size);
    }
}

/// <summary>
/// Error severity enum for categorization
/// </summary>
public enum ErrorSeverity
{
    Critical = 0,  // System/Infrastructure errors
    High = 1,      // Auth/Database errors
    Medium = 2,    // Validation/Input errors
    Low = 3        // Other runtime errors
}
