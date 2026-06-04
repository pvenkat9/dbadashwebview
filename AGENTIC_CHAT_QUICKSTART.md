# Quick Start: Trimble Agentic Chat Integration

## Files Created

### Backend
- `backend/Services/AgenticChatConfiguration.cs` - Configuration models
- `backend/Services/DbaAgenticChatProvider.cs` - Provider implementation
- `backend/Services/DbaAgenticChatEventHandlers.cs` - Event/exception handlers
- `backend/Controllers/AgenticChatController.cs` - API endpoints
- `backend/DBADashWebView.csproj` - Updated with NuGet packages
- `backend/Program.cs` - Updated with service registration

### Frontend
- `frontend/public/agentic-chat-shell.html` - HTML shell with message bridge
- `frontend/src/components/AgenticChat.tsx` - React component
- `frontend/src/components/AgenticChat.css` - Component styling
- `frontend/src/pages/AgenticChatExamples.tsx` - Usage examples

### Configuration
- `nuget.config` - Trimble Agentic NuGet feed
- `backend/appsettings.json` - Chat configuration

## Setup Steps

### 1. Install Dependencies

```bash
# Backend
cd backend
dotnet restore

# Frontend
cd frontend
npm install
```

### 2. Configure Agent

Edit `backend/appsettings.json`:

```json
"AgenticChat": {
  "AgentId": "YOUR_AGENT_UUID_HERE",
  "Environment": "Stage"
}
```

### 3. Add Component to Your App

In your React router or page component:

```tsx
import AgenticChat from './components/AgenticChat';

<div style={{ width: '100%', height: '600px' }}>
  <AgenticChat />
</div>
```

### 4. Ensure Authentication

Store the JWT token in localStorage:

```javascript
localStorage.setItem('authToken', jwtToken);
```

Or set globally:

```javascript
window.__AGENTIC_TOKEN__ = jwtToken;
```

## API Endpoints

All endpoints require JWT authentication:

- `GET /api/chat/config` - Get current configuration
- `POST /api/chat/message` - Handle Chat UI messages
- `POST /api/chat/config/update` - Update configuration
- `GET /api/chat/health` - Health check

## Testing

1. **Backend**
   ```bash
   cd backend
   dotnet run
   ```

2. **Frontend**
   ```bash
   cd frontend
   npm run dev
   ```

3. **Verify**
   - Navigate to Chat page
   - Should see loading indicator
   - Chat UI should load from configured environment
   - Messages should pass through to backend

## Customization

### Add Custom Tools

In `backend/Services/DbaAgenticChatProvider.cs`, add tools in `ProvideRunConfig`:

```csharp
["tool_name"] = new RuntimeToolConfig
{
    Definition = new Tool
    {
        Name = "tool_name",
        Description = "What this tool does",
        InputSchema = new { /* schema */ }
    },
    Callback = async (args) =>
    {
        // Implement tool logic
        return JsonConvert.SerializeObject(result);
    },
    TimeOutInMs = 5000
}
```

### Update UI Theme/Layout

Edit `backend/appsettings.json`:

```json
"AgenticChat": {
  "Theme": "Dark",           // Light | Dark
  "Variant": "Minimal",      // Full | Minimal | Narrow
  "Locale": "de"             // de, fr, es, etc.
}
```

### Switch Environments

```json
"Environment": "Prod"  // Change from Stage to Prod
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Chat not loading | Check AgentId in appsettings.json |
| Auth error 401 | Verify JWT token in localStorage/window.__AGENTIC_TOKEN__ |
| Message not received | Check API endpoint `/api/chat/message` is accessible |
| CORS error | Verify CORS is configured in Program.cs |
| Blank iframe | Check browser console for errors |

## Documentation

- Full guide: [AGENTIC_CHAT_INTEGRATION.md](../AGENTIC_CHAT_INTEGRATION.md)
- Examples: [AgenticChatExamples.tsx](../frontend/src/pages/AgenticChatExamples.tsx)
- API Reference: See controller and provider classes

## Architecture Diagram

```
┌─────────────────────────────────────────────┐
│           DBA Dashboard Frontend            │
│                  React App                  │
├─────────────────────────────────────────────┤
│        AgenticChat Component (React)        │
│                                             │
│  ┌───────────────────────────────────────┐  │
│  │   HTML Chat Shell (iframe)            │  │
│  │  - Handles message passing            │  │
│  │  - Manages authentication             │  │
│  │  - Updates connection status          │  │
│  │                                       │  │
│  │  ┌─────────────────────────────────┐ │  │
│  │  │  Trimble Chat UI (remote)       │ │  │
│  │  │  - User interface              │ │  │
│  │  │  - Message composition         │ │  │
│  │  │  - AI interactions             │ │  │
│  │  └─────────────────────────────────┘ │  │
│  └───────────────────────────────────────┘  │
└─────────────────────────────────────────────┘
        ↓ (HTTP POST with JWT)
┌──────────────────────────────────┐
│    DBA Dashboard Backend         │
│      ASP.NET Core API            │
├──────────────────────────────────┤
│  AgenticChatController           │
│  /api/chat/message               │
│  /api/chat/config                │
│  /api/chat/config/update         │
├──────────────────────────────────┤
│  DbaAgenticChatProvider          │
│  - Config provisioning           │
│  - Token management              │
│  - Tool definitions              │
│  - Context injection             │
├──────────────────────────────────┤
│  Event & Exception Handlers      │
│  - Event logging                 │
│  - Error tracking                │
└──────────────────────────────────┘
        ↓ (JWT Auth)
┌──────────────────────────────────┐
│  Trimble Identity / Auth Server  │
└──────────────────────────────────┘
```

## Next Steps

1. ✅ Files are created and configured
2. 📦 Install NuGet packages: `dotnet restore`
3. 🔧 Replace `AgentId` with your actual UUID
4. ▶️ Run backend: `dotnet run`
5. ▶️ Run frontend: `npm run dev`
6. 🧪 Test the integration
7. 🎨 Customize styling and tools as needed

## Support & Documentation

- Trimble Agentic Chat Docs: https://docs.trimble.com/agentic-chat
- DBA Dashboard: See local README.md
- Issues: Check browser console and backend logs

---

**Integration Status**: ✅ Ready for testing

All files are in place. Next step is to replace the placeholder AgentId with your actual UUID and run the application.
