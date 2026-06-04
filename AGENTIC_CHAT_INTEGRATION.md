# Trimble Agentic Chat Integration Guide

This guide explains how to integrate the Trimble Agentic Chat UI into the DBA Dashboard application using iframe embedding with secure message bridge communication.

## Overview

The integration uses an iframe embedding process that:
- Loads the remote Chat UI in a local HTML shell
- Bridges communication between the iframe and backend via `postMessage` API
- Provides secure authentication and message passing
- Maintains separation of concerns between frontend, backend, and Chat UI

## Architecture

```
Frontend (React)
    ↓ (imports)
AgenticChat.tsx (React component)
    ↓ (displays)
agentic-chat-shell.html (HTML shell with message bridge)
    ↓ (iframe)
Trimble Agentic Chat UI (remote)
    ↓ (message passing)
Backend API (/api/chat/*)
    ↓ (processes)
AgenticChatProvider (implements IAgenticChatProvider)
    ↓ (returns)
Chat configuration, tokens, and run context
```

## Installation & Setup

### 1. Backend Configuration

#### Update NuGet Packages

The project has been updated with the following NuGet packages:
- `Trimble.AgenticChat.Core` - Core protocol implementation
- `Newtonsoft.Json` - JSON serialization

#### Add NuGet Feed

A `nuget.config` file has been created at the workspace root with the Trimble Agentic NuGet feed:

```xml
<add key="trimble-agentic-external-nuget-local"
     value="https://artifactory.trimble.tools/artifactory/api/nuget/v3/trimble-agentic-external-nuget-local" />
```

#### Restore Packages

```bash
cd backend
dotnet restore
```

### 2. Backend Services

Three new services have been created in `backend/Services/`:

- **`AgenticChatConfiguration.cs`** - Configuration models
- **`DbaAgenticChatProvider.cs`** - Implements `IAgenticChatProvider`
- **`DbaAgenticChatEventHandlers.cs`** - Event and exception handlers

#### Configure in appsettings.json

```json
"AgenticChat": {
  "Environment": "Stage",        // Stage | Prod
  "AgentId": "your-agent-uuid",  // Replace with your agent UUID
  "Theme": "Light",              // Light | Dark
  "Variant": "Full",             // Full | Minimal | Narrow
  "ContentVariant": "Chat",      // Chat | AgentCards
  "Locale": "en",                // BCP-47 locale tag
  "HideModelSelection": false,
  "OnBeforeRunTimeoutMs": 5000
}
```

### 3. Backend API Controller

The `AgenticChatController.cs` in `backend/Controllers/` provides the following endpoints:

- `POST /api/chat/message` - Handle incoming Chat UI messages
- `GET /api/chat/config` - Retrieve current Chat UI configuration
- `POST /api/chat/config/update` - Update configuration at runtime
- `GET /api/chat/health` - Health check endpoint

All endpoints require JWT authentication (except health check).

### 4. Frontend Components

#### HTML Chat Shell

`frontend/public/agentic-chat-shell.html` is the bridge between the Chat UI iframe and backend:
- Loads Chat UI in iframe
- Handles `postMessage` communication
- Manages authentication tokens
- Updates connection status

#### React Component

`frontend/src/components/AgenticChat.tsx` is the React component for integrating chat:
- Displays the HTML shell in an iframe
- Handles loading and error states
- Shows connection status
- Manages lifecycle

## Usage

### Basic Setup

1. **Update Agent UUID**

   In `backend/appsettings.json`, replace the placeholder:
   ```json
   "AgentId": "your-actual-agent-uuid-here"
   ```

2. **Add Component to Page**

   In your React application, import and use the component:
   ```tsx
   import AgenticChat from './components/AgenticChat';

   export function ChatPage() {
     return (
       <div style={{ width: '100%', height: '600px' }}>
         <AgenticChat />
       </div>
     );
   }
   ```

3. **Ensure Authentication**

   The component requires:
   - Valid JWT token in localStorage or sessionStorage with key `authToken`
   - Or token available as `window.__AGENTIC_TOKEN__`
   - Or extracted from Authorization header in backend

### Authentication Flow

1. User authenticates with DBA Dashboard
2. JWT token is stored in localStorage/sessionStorage
3. Chat shell retrieves token on initialization
4. Token is included in all API requests to `/api/chat/*`
5. Backend extracts token and passes to Chat UI

### Token Storage

The HTML shell looks for tokens in this order:
1. `localStorage.getItem('authToken')`
2. `sessionStorage.getItem('authToken')`
3. `window.__AGENTIC_TOKEN__`
4. Authorization header from backend context

## Customization

### Styling

The Chat component and shell are fully customizable:

- **Component CSS**: `frontend/src/components/AgenticChat.css`
- **Shell CSS**: Embedded in `frontend/public/agentic-chat-shell.html`

### Local Tools

Define custom tools that the AI can call in your application:

In `backend/Services/DbaAgenticChatProvider.cs`:

```csharp
["get_server_status"] = new RuntimeToolConfig
{
    Definition = new Tool
    {
        Name = "get_server_status",
        Description = "Returns SQL server status",
        InputSchema = new {
            type = "object",
            properties = new {
                instanceName = new { type = "string" }
            }
        }
    },
    Callback = async (args) =>
    {
        // Call your API to get server status
        return JsonConvert.SerializeObject(/* result */);
    },
    TimeOutInMs = 10000
}
```

### Run Context

Inject dynamic context data for the AI:

```csharp
var context = new OnBeforeRunConfig
{
    RunContext = new RunContext
    {
        Context = new List<ContextObject>
        {
            new ContextObject { Description = "Current User", Value = userName },
            new ContextObject { Description = "Application", Value = "DBA Dashboard" }
        }
    }
};
```

### Event Handling

Handle user interactions in `DbaAgenticChatEventHandler.cs`:

```csharp
public void OnEvent(ChatUiEvent evt)
{
    switch (evt.Type)
    {
        case "OnAgentSelect":
            // Handle agent selection
            break;
        case "OnNewChat":
            // Handle new chat
            break;
        // ... other event types
    }
}
```

### Exception Handling

Implement custom error handling in `DbaAgenticChatExceptionHandler.cs`:

```csharp
public void OnException(Exception exception)
{
    // Log to error tracking service
    // Send alerts to administrators
    // Track metrics
}
```

## Runtime Configuration Updates

Update the Chat UI configuration without reloading:

### From Backend

```csharp
_agenticChat.UpdateConfig(new ChatUiConfiguration
{
    Environment = Environment.Prod,
    AgentId = "another-agent-uuid",
    UiConfig = new UiConfig { Theme = Theme.Dark }
});
```

### From Frontend

```typescript
// In AgenticChat component
window.AgenticChatShell?.updateConfig({
  Environment: 'Prod',
  Theme: 'Dark'
});
```

## Security Considerations

### 1. Token Security

- Tokens are extracted from Authorization header or localStorage
- Never log or expose sensitive token data
- Implement token refresh mechanisms

### 2. Origin Validation

The shell validates message origins:
- Only accepts messages from configured Chat UI URLs
- Rejects messages from untrusted origins

### 3. CORS Configuration

The backend has CORS enabled to allow iframe communication:
```csharp
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
```

Consider restricting in production:
```csharp
.WithOrigins("https://yourdomain.com")
```

### 4. IFrame Sandbox

The iframe uses restricted sandbox attributes:
```html
<iframe sandbox="allow-same-origin allow-scripts allow-popups allow-forms"></iframe>
```

## Troubleshooting

### Chat UI Not Loading

1. **Check Agent UUID** - Ensure `AgentId` in appsettings.json is valid
2. **Verify Environment** - Check that environment (Stage/Prod) is correct
3. **Check Network** - Verify Chat UI URL is accessible
4. **Browser Console** - Look for error messages in browser developer tools

### Authentication Errors

1. **Token Not Found** - Ensure token is in localStorage/sessionStorage with key `authToken`
2. **Token Expired** - Implement token refresh in `OnUnauthorized()`
3. **Bearer Token Missing** - Check Authorization header in API requests

### Message Communication Failures

1. **Check Origins** - Verify Chat UI origin matches configuration
2. **API Endpoints** - Ensure `/api/chat/*` endpoints are accessible
3. **CORS** - Check CORS configuration in backend
4. **JWT Auth** - Verify JWT middleware is configured

### Performance Issues

1. **Timeout** - Increase `OnBeforeRunTimeout` if tools take longer
2. **Message Lag** - Check network latency
3. **Memory** - Monitor for memory leaks in chat sessions

## API Reference

### Message Protocol

#### Incoming Message (from iframe)
```javascript
{
  "type": "AGENTIC_MESSAGE",
  "payload": {
    // JSON protocol message from Chat UI
  }
}
```

#### Outgoing Response (to iframe)
```javascript
{
  "type": "AGENTIC_RESPONSE",
  "payload": {
    // JSON protocol response from backend
  }
}
```

#### Error Message (to iframe)
```javascript
{
  "type": "AGENTIC_ERROR",
  "payload": {
    "error": "Error message",
    "timestamp": "2024-05-19T10:30:00Z"
  }
}
```

## Environment Variables

Add these to your `.env` file:

```env
VITE_AGENTIC_API_URL=http://localhost:5000/api/chat
VITE_AGENTIC_ENVIRONMENT=Stage
VITE_AUTH_TOKEN_KEY=authToken
```

## Dependencies

- **Backend**: .NET 8.0
  - `Trimble.AgenticChat.Core` - Core protocol
  - `Newtonsoft.Json` - JSON serialization
  - `Microsoft.AspNetCore.Authentication.JwtBearer` - JWT auth

- **Frontend**: React + TypeScript
  - Standard React hooks (useEffect, useRef, useState)
  - CSS3 for styling

## Support

For issues or questions:
1. Check the troubleshooting section
2. Review browser console for errors
3. Check backend logs for API errors
4. Consult Trimble Agentic Chat documentation

## Next Steps

1. Replace `your-agent-uuid` with your actual agent ID
2. Deploy to your environment
3. Test message passing between Chat UI and backend
4. Implement custom tools for your use cases
5. Customize styling to match your application theme
