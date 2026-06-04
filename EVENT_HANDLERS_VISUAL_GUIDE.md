# Event Handlers: Visual Implementation Guide

## 🔄 How Events Flow Through Your System

```
┌─────────────────────────────────────────────────────────────────────┐
│                         User's Browser                              │
│                                                                      │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  Trimble Agentic Chat UI                                    │   │
│  │  - User starts new chat                                     │   │
│  │  - Sends: { type: "OnNewChat" }                             │   │
│  └──────────────────────┬──────────────────────────────────────┘   │
│                         │                                           │
│                         │ postMessage                               │
│                         ▼                                           │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  HTML Shell (agentic-chat-shell.html)                       │   │
│  │  - Receives message                                         │   │
│  │  - Sends to: POST /api/chat/message                         │   │
│  └──────────────────────┬──────────────────────────────────────┘   │
└─────────────────────────┼──────────────────────────────────────────┘
                          │ HTTP
                          ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    Your Backend (ASP.NET)                           │
│                                                                      │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  AgenticChatController.HandleChatMessage()                  │   │
│  │  - Receives message from shell                              │   │
│  │  - Passes to: AgenticChat.HandleReceivedMessageAsync()      │   │
│  └──────────────────────┬──────────────────────────────────────┘   │
│                         │                                           │
│                         ▼                                           │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  Trimble.AgenticChat.Core                                   │   │
│  │  - Processes the message                                    │   │
│  │  - Needs config, token, tools → calls Provider              │   │
│  │  - Gets run context → calls Provider                        │   │
│  │  - Internally calls: OnEvent(ChatUiEvent) ← HERE!           │   │
│  └──────────────────────┬──────────────────────────────────────┘   │
│                         │                                           │
│                         ▼                                           │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  DbaAgenticChatProvider.ProvideRunConfig()                  │   │
│  │  - Detects chat started                                     │   │
│  │  - Passes to: _eventHandler.OnEvent(...)                    │   │
│  └──────────────────────┬──────────────────────────────────────┘   │
│                         │                                           │
│                         ▼                                           │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  ⭐ DbaAgenticChatEventHandler.OnEvent()                    │   │
│  │                                                             │   │
│  │  ┌──────────────────────────────────────────────────────┐  │   │
│  │  │  switch (evt.Type) {                                │  │   │
│  │  │    case "OnNewChat":                                │  │   │
│  │  │      HandleNewChat(user, timestamp)                 │  │   │
│  │  │      ↓                                              │  │   │
│  │  │      _logger.LogInformation("Chat started")         │  │   │
│  │  │      // TODO: Add your logic here                   │  │   │
│  │  │      //   - Log to database                         │  │   │
│  │  │      //   - Send notification                       │  │   │
│  │  │      //   - Track metrics                           │  │   │
│  │  │      break;                                         │  │   │
│  │  │                                                     │  │   │
│  │  │    case "OnCreateAgent":                            │  │   │
│  │  │      HandleCreateAgent(user, agent, timestamp)      │  │   │
│  │  │      ↓                                              │  │   │
│  │  │      _logger.LogWarning("Agent created")            │  │   │
│  │  │      // TODO: Check permissions, send alert         │  │   │
│  │  │      break;                                         │  │   │
│  │  │  }                                                  │  │   │
│  │  └──────────────────────────────────────────────────────┘  │   │
│  └──────────────────────┬──────────────────────────────────────┘   │
│                         │                                           │
│                         ▼ (Your custom logic)                       │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  Your Database / External Services                          │   │
│  │  - ChatAuditLog table                                       │   │
│  │  - Send email alerts                                        │   │
│  │  - Update metrics                                           │   │
│  │  - Create security incidents                               │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 🎯 Where to Add Your Code

### **The 3 Insertion Points**

```csharp
// ============================================================
// INSERTION POINT #1: Event Handler
// ============================================================

public void OnEvent(ChatUiEvent evt)
{
    var userName = GetUserName();  // ← Already extracted
    var timestamp = DateTime.UtcNow;  // ← Already set

    switch (evt.Type)
    {
        case "OnNewChat":
            HandleNewChat(userName, evt.Payload, timestamp);
            break;
    }
}

// ============================================================
// INSERTION POINT #2: Individual Event Method
// ============================================================

private void HandleNewChat(string userName, string? payload, DateTime timestamp)
{
    _logger.LogInformation("Chat started");
    
    // ⭐ ADD YOUR CODE HERE ⭐
    // Examples:
    // 1. Log to database
    //    using var conn = new SqlConnection(...);
    //    conn.Open();
    //    INSERT INTO ChatLog ...
    //
    // 2. Send alert
    //    if (IsAdmin(userName)) SendAlert(...);
    //
    // 3. Update cache
    //    cache.Set($"user:{userName}:active", true);
    //
    // 4. Start timer
    //    _sessionStart[userName] = timestamp;
}

// ============================================================
// INSERTION POINT #3: Exception Handler
// ============================================================

private void HandleCriticalError(string userName, Exception exception, DateTime timestamp)
{
    _logger.LogCritical(exception, "Critical error");
    
    // ⭐ ADD YOUR CODE HERE ⭐
    // Examples:
    // 1. Send to error tracking
    //    SentrySdk.CaptureException(exception);
    //
    // 2. Alert admins
    //    SendAlert("CRITICAL: " + exception.Message);
    //
    // 3. Disable chat for user
    //    DisableChatForUser(userName);
    //
    // 4. Create incident ticket
    //    TicketingService.CreateTicket(...);
}
```

---

## 📊 Event Handler Reference Card

### **Quick Lookup: What Fires When?**

```
┌─────────────────────┬──────────────────────┬─────────────────────┐
│ Event               │ When It Fires         │ Suggested Action    │
├─────────────────────┼──────────────────────┼─────────────────────┤
│ OnNewChat           │ User starts chatting │ Log session start   │
│ OnChatClose         │ User closes chat     │ Log session end     │
│ OnAgentSelect       │ User picks agent     │ Track preference    │
│ OnCreateAgent       │ User creates agent   │ ALERT! (security)   │
│ OnThreadSelect      │ User resumes thread  │ Load context        │
│ OnExploreAgents     │ User browsing agents │ Track exploration   │
│ OnSignIn            │ User logs in         │ Initialize session  │
│ OnMyTrimbleClick    │ User clicks link     │ Track navigation    │
│ OnChatInputButton   │ User clicks button   │ Track action        │
└─────────────────────┴──────────────────────┴─────────────────────┘

Priority Implementation Order:
1️⃣ OnCreateAgent - SECURITY
2️⃣ OnNewChat - TRACKING
3️⃣ OnChatClose - CLEANUP
4️⃣ Others - NICE TO HAVE
```

---

## 🔗 Connection Map

```
Your Code Structure:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Program.cs (Registration)
  └─ builder.Services.AddScoped<IAgenticChatEventHandler, DbaAgenticChatEventHandler>()

DbaAgenticChatProvider.cs (Injection)
  └─ Uses: IAgenticChatEventHandler _eventHandler
       └─ Calls: _eventHandler.OnEvent(chatUiEvent)

DbaAgenticChatEventHandlers.cs (Implementation)
  ├─ DbaAgenticChatEventHandler
  │  ├─ OnEvent() - Router
  │  ├─ HandleNewChat() - Your code here
  │  ├─ HandleChatClose() - Your code here
  │  ├─ HandleCreateAgent() - Your code here
  │  └─ ... 6 more methods
  │
  ├─ DbaAgenticChatExceptionHandler
  │  ├─ OnException() - Error router
  │  ├─ HandleCriticalError() - Your code here
  │  ├─ HandleHighSeverityError() - Your code here
  │  └─ HandleLowSeverityError() - Your code here
  │
  └─ DbaRawAgenticChatMessageHandler
     └─ OnRawMessage() - Debug messages

Your External Services (Options)
  ├─ Database
  ├─ Email Service
  ├─ Error Tracking (Sentry)
  ├─ Notification Service (Slack, Teams)
  └─ Metrics/Analytics Service
```

---

## 💻 Minimal Viable Implementation

**To get something working in 5 minutes:**

```csharp
// 1. Open DbaAgenticChatEventHandlers.cs

// 2. Find HandleNewChat() method (around line 110)

// 3. Replace the empty TODO section with:

private void HandleNewChat(string userName, DateTime timestamp)
{
    _logger.LogInformation("✅ Chat session started for {User} at {Time}", 
        userName, timestamp);
    
    // That's it! Check your logs and you'll see this message.
}

// 4. Run your app:
// cd backend
// dotnet run

// 5. Start a new chat - check console logs
// You should see: "✅ Chat session started for myusername at 2024-05-19..."
```

---

## 🎓 Learning Path

```
Day 1: Understanding
├─ Read: EVENT_HANDLERS_QUICK_GUIDE.md (5 min)
├─ Read: This file (10 min)
└─ Read: How events flow above (5 min)
   Total: 20 minutes

Day 2: Implementation
├─ Implement: HandleNewChat() (5 min)
├─ Test: Start a chat, see logs (5 min)
├─ Implement: HandleCreateAgent() (10 min)
└─ Test: Try creating something (5 min)
   Total: 25 minutes

Day 3: Database
├─ Create: ChatLog table (10 min)
├─ Add: DB insert code to HandleNewChat() (15 min)
├─ Test: Verify inserts work (10 min)
└─ Celebrate: You have persistent logging! 🎉
   Total: 35 minutes

Week 1: Polish
├─ Implement: 3-5 more event handlers
├─ Add: Error tracking (optional)
├─ Add: Alerts for critical operations
└─ Run: In production!
```

---

## ✅ Final Checklist

- [ ] Read this file
- [ ] Understand where events come from (Chat UI)
- [ ] Understand where they go (Your code)
- [ ] Pick one event to implement
- [ ] Add 2-3 lines of code
- [ ] Test it works
- [ ] Add next event
- [ ] Repeat

---

## 🚀 You're Ready!

You now understand:
- ✅ How events flow from Chat UI → Your code
- ✅ Which events exist and when they fire
- ✅ Where to add your custom logic
- ✅ What the 3 handlers do
- ✅ How to test your implementation

**Next step:** Go to `EVENT_HANDLERS_QUICK_GUIDE.md` and pick ONE event to implement.

**Good luck!** 🎉
