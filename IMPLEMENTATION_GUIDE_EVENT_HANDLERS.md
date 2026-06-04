# DbaAgenticChatEventHandlers Implementation Guide

## 📋 Overview

The `DbaAgenticChatEventHandlers.cs` file contains three handler classes for managing Chat UI interactions, errors, and debugging:

1. **DbaAgenticChatEventHandler** - Tracks user interactions
2. **DbaAgenticChatExceptionHandler** - Handles errors
3. **DbaRawAgenticChatMessageHandler** - Debug raw messages

---

## 🎯 Handler 1: DbaAgenticChatEventHandler

### **What It Does**
Triggered when users interact with the Chat UI. Each event type needs custom business logic.

### **Event Types**

| Event | When It Fires | Use Cases |
|-------|---------------|-----------|
| `OnAgentSelect` | User selects an AI agent | Track agent usage, audit access |
| `OnThreadSelect` | User resumes a conversation | Load context, analytics |
| `OnNewChat` | New conversation started | Create session, initialize |
| `OnExploreAgents` | User browsing available agents | Track user behavior, recommendations |
| `OnCreateAgent` | User creates new agent | Compliance logging, permissions check |
| `OnMyTrimbleClick` | External link clicked | Navigation tracking |
| `OnChatInputButtonClick` | Custom button clicked | Action tracking |
| `OnSignIn` | User authenticated | Session start, preferences load |
| `OnClose` | Chat closed | Session cleanup, transcript save |

### **Implementation Example 1: Audit Logging to Database**

```csharp
// Add to HandleAgentSelect method
private void HandleAgentSelect(string userName, string? payload, DateTime timestamp)
{
    _logger.LogInformation(
        "[AUDIT] User {UserName} selected agent. Payload: {Payload}",
        userName, payload);

    // IMPLEMENT: Log to audit table
    try
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        
        var cmd = new SqlCommand(
            @"INSERT INTO ChatAuditLog (UserId, Action, AgentId, Timestamp, IpAddress)
              VALUES (@userId, @action, @agentId, @timestamp, @ipAddress)", conn);
        
        cmd.Parameters.AddWithValue("@userId", userName);
        cmd.Parameters.AddWithValue("@action", "AgentSelect");
        cmd.Parameters.AddWithValue("@agentId", payload ?? "");
        cmd.Parameters.AddWithValue("@timestamp", timestamp);
        cmd.Parameters.AddWithValue("@ipAddress", GetClientIpAddress());
        
        await cmd.ExecuteNonQueryAsync();
        _logger.LogInformation("Audit log recorded for {UserName}", userName);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to log audit for agent selection");
    }
}
```

### **Implementation Example 2: Send Alert for Critical Operations**

```csharp
// Add to HandleCreateAgent method
private void HandleCreateAgent(string userName, string? payload, DateTime timestamp)
{
    _logger.LogWarning(
        "[CRITICAL] User {UserName} attempting to create agent",
        userName);

    // IMPLEMENT: Check permissions
    if (!HasPermission(userName, "create_agent"))
    {
        _logger.LogWarning("[SECURITY] Unauthorized agent creation attempt by {UserName}", userName);
        
        // Send alert to admins
        SendAdminAlert(
            subject: "⚠️ Unauthorized Agent Creation Attempt",
            message: $"User {userName} attempted to create agent at {timestamp}\n\nAgent Details: {payload}");
        
        // Create security incident
        LogSecurityIncident(userName, "UnauthorizedAgentCreation", payload);
        return;
    }

    // If authorized, log the creation
    LogAgentCreation(userName, payload, timestamp);
}

private bool HasPermission(string userName, string permission)
{
    // IMPLEMENT: Check user roles against permissions
    // return _userPermissionService.HasPermission(userName, permission);
    return true; // Placeholder
}

private void SendAdminAlert(string subject, string message)
{
    // IMPLEMENT: Send email/Slack/Teams notification
    // _emailService.SendToAdmins(subject, message);
    // _slackService.PostAlert(message);
}
```

### **Implementation Example 3: Session Tracking**

```csharp
// Add to HandleNewChat method
private void HandleNewChat(string userName, DateTime timestamp)
{
    _logger.LogInformation("[SESSION] New chat started for {UserName}", userName);

    try
    {
        // Create session record
        var sessionId = Guid.NewGuid().ToString();
        
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        
        var cmd = new SqlCommand(
            @"INSERT INTO ChatSessions (SessionId, UserId, StartTime, Status)
              VALUES (@sessionId, @userId, @startTime, @status)", conn);
        
        cmd.Parameters.AddWithValue("@sessionId", sessionId);
        cmd.Parameters.AddWithValue("@userId", userName);
        cmd.Parameters.AddWithValue("@startTime", timestamp);
        cmd.Parameters.AddWithValue("@status", "Active");
        
        await cmd.ExecuteNonQueryAsync();
        
        // Store in cache for easy access
        _cache.Set($"session:{sessionId}", new { userId = userName, startTime = timestamp }, 
            TimeSpan.FromHours(8));
        
        _logger.LogInformation("Session {SessionId} created for {UserName}", sessionId, userName);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to create chat session");
    }
}
```

---

## 🚨 Handler 2: DbaAgenticChatExceptionHandler

### **What It Does**
Catches and handles errors that occur during Chat UI operations. Severity is categorized automatically.

### **Error Severity Levels**

```
CRITICAL (0)   → System failures, immediate action needed
    ↓
HIGH (1)       → Auth/Database issues, needs attention  
    ↓
MEDIUM (2)     → Validation errors, can be handled
    ↓
LOW (3)        → Non-critical issues, just track
```

### **Implementation Example 1: Send to Error Tracking Service**

```csharp
// Update HandleCriticalError method
private void HandleCriticalError(string userName, Exception exception, DateTime timestamp)
{
    _logger.LogCritical(exception, "[CRITICAL] Chat error for {UserName}", userName);

    // IMPLEMENT: Send to Sentry for tracking
    var sentryClient = new SentryClient("your-sentry-dsn");
    sentryClient.CaptureException(exception, scope =>
    {
        scope.SetTag("severity", "critical");
        scope.SetTag("module", "chat-ui");
        scope.SetUser(new SentryUser { Username = userName });
        scope.SetExtra("timestamp", timestamp);
    });

    // IMPLEMENT: Send immediate notification to admins
    _notificationService.SendCriticalAlert(
        title: "🚨 Critical Chat Error",
        message: $"User: {userName}\nError: {exception.Message}\nTime: {timestamp}");

    // IMPLEMENT: Create incident ticket
    _ticketingService.CreateIncident(new IncidentTicket
    {
        Title = "Critical Chat UI Error",
        Priority = "P1",
        Description = exception.ToString(),
        AssignedUser = "on-call-engineer",
        Timestamp = timestamp
    });

    // IMPLEMENT: Optionally disable chat for safety
    DisableChatForUser(userName, "Critical error detected. Please try again later.");
}
```

### **Implementation Example 2: Database Connection Error Handling**

```csharp
// Update HandleHighSeverityError method
private void HandleHighSeverityError(string userName, Exception exception, DateTime timestamp)
{
    if (exception is SqlException sqlEx)
    {
        _logger.LogError(sqlEx, "[DB ERROR] SQL error for {UserName}", userName);

        // IMPLEMENT: Check if it's a transient error
        if (IsTransientSqlError(sqlEx))
        {
            // Retry logic
            _logger.LogInformation("Retrying operation due to transient error");
            RetryOperation(); // Implement retry
        }
        else
        {
            // Log to persistent error store
            LogDatabaseError(userName, sqlEx, timestamp);
            
            // Send alert if critical database is down
            if (sqlEx.Number == -2) // Connection timeout
            {
                SendAdminAlert("Database Connection Failed", 
                    $"Cannot connect to DBA Dashboard database. Last error: {sqlEx.Message}");
            }
        }
    }

    // IMPLEMENT: Log to database
    using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();
    
    var cmd = new SqlCommand(
        @"INSERT INTO ErrorLog (UserId, ErrorMessage, ErrorType, Severity, Timestamp)
          VALUES (@userId, @message, @type, @severity, @timestamp)", conn);
    
    cmd.Parameters.AddWithValue("@userId", userName);
    cmd.Parameters.AddWithValue("@message", exception.Message);
    cmd.Parameters.AddWithValue("@type", exception.GetType().Name);
    cmd.Parameters.AddWithValue("@severity", "High");
    cmd.Parameters.AddWithValue("@timestamp", timestamp);
    
    await cmd.ExecuteNonQueryAsync();
}

private bool IsTransientSqlError(SqlException ex)
{
    // Transient errors that can be retried
    var transientErrorNumbers = new[] { -2, -1, 2, 20, 64, 233, 4221 };
    return transientErrorNumbers.Contains(ex.Number);
}
```

### **Implementation Example 3: Monitoring and Alerting**

```csharp
// Add new method to monitor error trends
private void MonitorErrorTrends()
{
    try
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();
        
        var cmd = new SqlCommand(
            @"SELECT Severity, COUNT(*) as Count 
              FROM ErrorLog 
              WHERE Timestamp > DATEADD(hour, -1, GETUTCDATE())
              GROUP BY Severity", conn);
        
        using var reader = await cmd.ExecuteReaderAsync();
        
        int criticalCount = 0;
        while (await reader.ReadAsync())
        {
            var severity = reader["Severity"].ToString();
            var count = (int)reader["Count"];
            
            if (severity == "Critical")
                criticalCount = count;
        }
        
        // Alert if too many critical errors
        if (criticalCount > 5)
        {
            SendAdminAlert(
                "⚠️ High Error Rate Detected",
                $"5+ critical errors in the last hour. Please investigate.");
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error monitoring trends");
    }
}
```

---

## 🔍 Handler 3: DbaRawAgenticChatMessageHandler

### **What It Does**
Receives every JSON message for debugging. ⚠️ **Never log sensitive data like tokens!**

### **Implementation Example 1: Message Pattern Analysis**

```csharp
// Update OnRawMessage method
public void OnRawMessage(string json, bool isIncoming)
{
    var direction = isIncoming ? "INCOMING" : "OUTGOING";
    var summary = ExtractMessageSummary(json);
    
    _logger.LogDebug("[MSG] {Direction}: {Summary}", direction, summary);

    // IMPLEMENT: Track message patterns
    AnalyzeMessagePattern(json, isIncoming);
    
    // IMPLEMENT: Monitor performance
    MonitorMessageSize(json);
    
    // IMPLEMENT: Track tool calls
    TrackToolCalls(json);
}

private void AnalyzeMessagePattern(string json, bool isIncoming)
{
    try
    {
        // Extract message type without logging full content
        var pattern = System.Text.RegularExpressions.Regex.Match(
            json, @"""type""\s*:\s*""([^""]+)""");
        
        if (pattern.Success)
        {
            var messageType = pattern.Groups[1].Value;
            _logger.LogDebug("Message type: {Type}", messageType);
            
            // IMPLEMENT: Track message type frequency
            _metrics.IncrementCounter($"message_type:{messageType}");
            
            // Alert on unusual patterns
            if (messageType == "ERROR")
            {
                _logger.LogWarning("Error message received");
                // _notificationService.AlertOnError();
            }
        }
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Error analyzing message pattern");
    }
}

private void MonitorMessageSize(string json)
{
    var sizeInBytes = System.Text.Encoding.UTF8.GetByteCount(json);
    var sizeInKb = sizeInBytes / 1024.0;
    
    // IMPLEMENT: Record metrics
    _metrics.RecordGauge("message_size_bytes", sizeInBytes);
    
    // Alert on excessively large messages
    if (sizeInKb > 500) // 500KB threshold
    {
        _logger.LogWarning("Large message detected: {SizeInKb} KB", sizeInKb);
        // _notificationService.AlertLargeMessage(sizeInKb);
    }
}

private void TrackToolCalls(string json)
{
    // Look for tool execution patterns
    if (json.Contains("\"tool\"") && json.Contains("\"callback\""))
    {
        var toolMatch = System.Text.RegularExpressions.Regex.Match(
            json, @"""tool""\s*:\s*""([^""]+)""");
        
        if (toolMatch.Success)
        {
            var toolName = toolMatch.Groups[1].Value;
            _logger.LogDebug("Tool called: {ToolName}", toolName);
            
            // IMPLEMENT: Track tool performance
            _metrics.IncrementCounter($"tool_call:{toolName}");
        }
    }
}
```

### **Implementation Example 2: Development Debug Logging**

```csharp
// Add conditional logging for development only
public void OnRawMessage(string json, bool isIncoming)
{
    var direction = isIncoming ? "INCOMING" : "OUTGOING";

#if DEBUG
    // In DEBUG mode, log more details (but still exclude tokens)
    var cleanedJson = RemoveSensitiveData(json);
    _logger.LogDebug("Full {Direction} message: {Json}", direction, cleanedJson);
    
    // IMPLEMENT: Performance tracing
    if (!isIncoming)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        // Message sending time measurement
        _logger.LogDebug("Message sent in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
    }
#else
    // In RELEASE mode, only log summary
    var summary = ExtractMessageSummary(json);
    _logger.LogDebug("[MSG] {Direction}: {Summary}", direction, summary);
#endif
}

private string RemoveSensitiveData(string json)
{
    // Remove authentication tokens and sensitive data
    var cleaned = System.Text.RegularExpressions.Regex.Replace(
        json, @"""(token|password|secret|authorization)""\s*:\s*""[^""]+""",
        @"""$1"": ""***REDACTED***""");
    
    return cleaned;
}
```

---

## 📊 Database Schema (If Implementing DB Logging)

```sql
-- Chat Sessions Table
CREATE TABLE ChatSessions (
    SessionId NVARCHAR(36) PRIMARY KEY,
    UserId NVARCHAR(255) NOT NULL,
    StartTime DATETIME2 NOT NULL,
    EndTime DATETIME2 NULL,
    Status NVARCHAR(50),
    MessageCount INT DEFAULT 0
);

-- Chat Audit Log Table
CREATE TABLE ChatAuditLog (
    AuditId INT IDENTITY(1,1) PRIMARY KEY,
    UserId NVARCHAR(255) NOT NULL,
    Action NVARCHAR(100) NOT NULL,
    AgentId NVARCHAR(255),
    Timestamp DATETIME2 NOT NULL,
    IpAddress NVARCHAR(45),
    Details NVARCHAR(MAX)
);

-- Error Log Table
CREATE TABLE ErrorLog (
    ErrorId INT IDENTITY(1,1) PRIMARY KEY,
    UserId NVARCHAR(255),
    ErrorMessage NVARCHAR(MAX),
    ErrorType NVARCHAR(100),
    Severity NVARCHAR(50),
    Timestamp DATETIME2,
    StackTrace NVARCHAR(MAX),
    IsResolved BIT DEFAULT 0
);

-- Create indexes for performance
CREATE INDEX IX_ChatAuditLog_UserId ON ChatAuditLog(UserId);
CREATE INDEX IX_ChatAuditLog_Timestamp ON ChatAuditLog(Timestamp);
CREATE INDEX IX_ErrorLog_Severity ON ErrorLog(Severity);
CREATE INDEX IX_ErrorLog_Timestamp ON ErrorLog(Timestamp);
```

---

## 🚀 Quick Implementation Checklist

- [ ] Choose which events to track (start with OnNewChat, OnClose, OnCreateAgent)
- [ ] Decide on error tracking service (Sentry, App Insights, DataDog, etc.)
- [ ] Create database tables for audit/error logging (optional)
- [ ] Set up notification channels (email, Slack, Teams)
- [ ] Implement at least one handler method completely
- [ ] Test with sample messages
- [ ] Monitor logs for 24 hours
- [ ] Iterate and add more handlers as needed

---

## 🔐 Security Best Practices

✅ **DO:**
- Log user actions for audit trails
- Track error rates and patterns
- Alert on unusual activities
- Remove sensitive data from logs

❌ **DON'T:**
- Log authentication tokens
- Log passwords or API keys
- Store raw message JSON with sensitive data
- Log personally identifiable information (PII) unnecessarily

---

## 📝 Summary

The three handlers work together:
1. **Event Handler** → Tracks what users do
2. **Exception Handler** → Catches and responds to errors
3. **Raw Message Handler** → Debugs communication issues

Start simple with just logging, then add database tracking and alerts as needed.
