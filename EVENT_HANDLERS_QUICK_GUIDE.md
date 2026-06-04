# Quick Reference: Event Handlers Implementation

## 📌 Minimum Viable Implementation

Start with THIS if you want the simplest working version:

### **Step 1: Update DbaAgenticChatEventHandler Constructor**

```csharp
public class DbaAgenticChatEventHandler : IAgenticChatEventHandler
{
    private readonly ILogger<DbaAgenticChatEventHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _connectionString; // Add this
    
    public DbaAgenticChatEventHandler(
        ILogger<DbaAgenticChatEventHandler> logger,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration) // Add this parameter
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _connectionString = configuration.GetConnectionString("DBADashDB")!; // Add this
    }
```

### **Step 2: Implement the SIMPLEST Event Handler**

Replace the entire `HandleNewChat()` with:

```csharp
private void HandleNewChat(string userName, DateTime timestamp)
{
    _logger.LogInformation("[SESSION_START] User: {User}, Time: {Time}", userName, timestamp);

    // That's it! Just logging. Later you can add database tracking.
}
```

### **Step 3: Implement the MOST IMPORTANT Event Handler**

Replace `HandleCreateAgent()` with this to catch dangerous operations:

```csharp
private void HandleCreateAgent(string userName, string? payload, DateTime timestamp)
{
    _logger.LogWarning("[SECURITY_ALERT] User {User} creating agent: {Agent}", userName, payload);

    // Optional: Add permission check
    // If you have a permission system, call it here
    // if (!HasCreateAgentPermission(userName)) { LogSecurityIncident(...); return; }
    
    _logger.LogInformation("Agent creation logged for audit trail");
}
```

### **Step 4: Test It Works**

1. Open your browser and navigate to the Chat page
2. Start a new chat
3. Check the logs - you should see: `[SESSION_START] User: ...`

---

## 📊 Example: Real-World Scenario

### **Scenario: Track Admin Operations**

```csharp
// In HandleCreateAgent() - Alert when admins do important things
private void HandleCreateAgent(string userName, string? payload, DateTime timestamp)
{
    var isAdmin = IsUserAdmin(userName);
    var logLevel = isAdmin ? LogLevel.Information : LogLevel.Warning;
    
    _logger.Log(
        logLevel,
        "[ADMIN_ACTION] User {User} (Admin={IsAdmin}) creating agent: {Agent}",
        userName, isAdmin, payload);

    // OPTIONAL: Send email notification
    if (isAdmin)
    {
        SendEmailToSecurityTeam(
            subject: $"Admin Operation - Agent Creation by {userName}",
            body: $"Agent: {payload}\nTime: {timestamp}");
    }
}

private bool IsUserAdmin(string userName)
{
    // TODO: Check your permission system
    // return _permissionService.IsAdmin(userName);
    return userName.Contains("admin", StringComparison.OrdinalIgnoreCase);
}

private void SendEmailToSecurityTeam(string subject, string body)
{
    // TODO: Implement email sending
    _logger.LogInformation("[EMAIL] {Subject}: {Body}", subject, body);
}
```

---

## 🔄 Progression Path: Start Simple, Add Complexity

### **Level 1: Just Logging (Recommended Start)**
```csharp
private void HandleNewChat(string userName, DateTime timestamp)
{
    _logger.LogInformation("Chat started for {User}", userName);
}
```
✅ Works immediately | Takes 1 minute to implement

---

### **Level 2: Add Key Events**
```csharp
// Track 3 important events
private void HandleNewChat(string userName, DateTime timestamp)
{
    _logger.LogInformation("[START] {User}", userName);
}

private void HandleChatClose(string userName, DateTime timestamp)
{
    _logger.LogInformation("[END] {User}", userName);
}

private void HandleCreateAgent(string userName, string? payload, DateTime timestamp)
{
    _logger.LogWarning("[ALERT] {User} creating {Agent}", userName, payload);
}
```
✅ Tracks user sessions and alerts on important actions

---

### **Level 3: Add Database Logging**
```csharp
private void HandleNewChat(string userName, DateTime timestamp)
{
    _logger.LogInformation("Chat for {User}", userName);
    
    try
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();
        
        var cmd = new SqlCommand(
            "INSERT INTO ChatLog (UserId, StartTime) VALUES (@user, @time)",
            conn);
        cmd.Parameters.AddWithValue("@user", userName);
        cmd.Parameters.AddWithValue("@time", timestamp);
        cmd.ExecuteNonQuery();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "DB error");
    }
}
```
✅ Persists data | Can run reports

---

### **Level 4: Add Alerts and Notifications**
```csharp
private void HandleCreateAgent(string userName, string? payload, DateTime timestamp)
{
    if (!IsUserAdmin(userName))
    {
        _logger.LogWarning("[SECURITY] Unauthorized: {User}", userName);
        SendAlert($"Security: {userName} attempted agent creation");
        return;
    }
    
    _logger.LogInformation("[ADMIN] {User} created {Agent}", userName, payload);
}
```
✅ Security monitoring | Real-time alerts

---

## 🎯 Event Handler Quick Reference

### **Which events to implement first?**

| Priority | Event | Why | Effort |
|----------|-------|-----|--------|
| 🔴 HIGH | `OnCreateAgent` | Security - block unauthorized access | ⭐ |
| 🔴 HIGH | `OnNewChat` | Usage tracking | ⭐ |
| 🟠 MEDIUM | `OnChatClose` | Session duration | ⭐⭐ |
| 🟠 MEDIUM | `OnAgentSelect` | Agent popularity | ⭐⭐ |
| 🟡 LOW | `OnExploreAgents` | Analytics | ⭐⭐⭐ |
| 🟢 OPTIONAL | Others | Nice to have | ⭐⭐⭐ |

---

## 📝 Copy-Paste Templates

### **Template 1: Simple Alert**
```csharp
private void HandleXXXEvent(string userName, string? payload, DateTime timestamp)
{
    _logger.LogInformation("[TAG] Message here", userName, payload);
    
    if (NeedsAlert(payload))
    {
        SendAlert($"Alert: {payload} by {userName}");
    }
}
```

### **Template 2: Database Insert**
```csharp
private void HandleXXXEvent(string userName, string? payload, DateTime timestamp)
{
    try
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();
        
        var cmd = new SqlCommand(
            "INSERT INTO LogTable (UserId, Data, Time) VALUES (@user, @data, @time)",
            conn);
        cmd.Parameters.AddWithValue("@user", userName);
        cmd.Parameters.AddWithValue("@data", payload ?? "");
        cmd.Parameters.AddWithValue("@time", timestamp);
        cmd.ExecuteNonQuery();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Insert failed");
    }
}
```

### **Template 3: Conditional Logic**
```csharp
private void HandleXXXEvent(string userName, string? payload, DateTime timestamp)
{
    if (IsAdmin(userName))
    {
        // Admin action - log it
        _logger.LogInformation("[ADMIN] {Action}", payload);
    }
    else if (IsUserRestricted(userName))
    {
        // Restricted user - alert
        _logger.LogWarning("[RESTRICTED] {User} attempted {Action}", userName, payload);
    }
    else
    {
        // Regular user - track
        _logger.LogDebug("[USER] {User} did {Action}", userName, payload);
    }
}
```

---

## 🚨 Exception Handler Implementation

### **Critical Errors Only**
```csharp
private void HandleCriticalError(string userName, Exception exception, DateTime timestamp)
{
    _logger.LogCritical(exception, "[CRITICAL] {User} error: {Message}", 
        userName, exception.Message);
    
    // Send immediate alert
    SendAlert($"🚨 CRITICAL ERROR: {exception.Message}");
}
```

### **All Errors with Categorization**
```csharp
private void HandleCriticalError(string userName, Exception exception, DateTime timestamp)
    => LogAndAlert(userName, exception, "CRITICAL", "urgent");

private void HandleHighSeverityError(string userName, Exception exception, DateTime timestamp)
    => LogAndAlert(userName, exception, "HIGH", "warning");

private void LogAndAlert(string user, Exception ex, string level, string alertLevel)
{
    _logger.LogError(ex, "[{Level}] Error for {User}: {Message}", level, user, ex.Message);
    
    if (alertLevel == "urgent")
        SendAlert($"🚨 {ex.Message}");
}
```

---

## ✅ Implementation Checklist

- [ ] Add `_httpContextAccessor` to constructor
- [ ] Add `_connectionString` to constructor  
- [ ] Implement `HandleNewChat()` - log session start
- [ ] Implement `HandleCreateAgent()` - alert on creation
- [ ] Implement `HandleChatClose()` - log session end
- [ ] Test: Start a new chat, see logs
- [ ] Test: Create agent, see alert
- [ ] Test: Close chat, see end log
- [ ] Add database table (if desired)
- [ ] Add email alerts (if desired)

---

## 🔧 Configuration in appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "DBADashWebView.Services.DbaAgenticChatEventHandler": "Debug"
    }
  }
}
```

Now in your logs you'll see all event handler logs!

---

## 📞 Common Issues & Solutions

| Issue | Cause | Solution |
|-------|-------|----------|
| Not seeing logs | Wrong log level | Set to `Debug` in appsettings |
| DB insert fails | Connection string wrong | Check: `GetConnectionString("DBADashDB")` |
| Null reference error | Missing `HttpContextAccessor` | Inject in constructor |
| Email not sent | Method not implemented | Replace with actual email service |
| Performance slow | Doing too much in handler | Make handlers async in future |

---

## 🎓 Next Steps

1. ✅ Implement Level 1 (just logging)
2. ✅ Test it works
3. ✅ Add Level 2 (key events)
4. ✅ Add database (Level 3) if needed
5. ✅ Add alerts (Level 4) for security

**That's it!** You now have event handlers working.
