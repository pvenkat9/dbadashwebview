# Match embedded chat to Trimble Agent Studio

The embed uses the **same** Trimble iframe and **same** agent id as Studio. To behave the same, align these three layers.

## 1. Same agent (you already have this)

- **Agent id:** `8703e7ea-23b4-464b-a40c-d1901ee0c0ae`
- **Environment:** `Stage`
- **Publish** the agent in Studio after any instruction/tool change.

## 2. DBA Dash backend — Agent Studio parity mode (code)

In `appsettings.json`:

```json
"AgentStudioParityMode": true,
"RegisterLocalRuntimeTools": false
```

This means:

- No `get_current_user` / `get_server_status` from DBA Dash
- No extra run context (no injected Current User / Application / guides)
- Agent uses **only** what you configured in Studio (`dbadash`, instructions, etc.)

Deploy latest `dbadash-webview.zip` and recycle IIS.

## 3. Same Trimble user token as Studio (critical)

| Studio | Embed (default) |
|--------|------------------|
| Your **Trimble user** session | **Client credentials** (`agents` scope) |

Client credentials often behave differently from Studio for `dbadash` and tool choice (`op: list` vs `exec`).

### Option A — Test parity quickly

1. In Studio/Assist, DevTools → Network → copy **Bearer** token.
2. Open chat:

   `https://YOUR-SITE/agentic-chat-shell.html?trimbleToken=PASTE_TOKEN`

3. New chat → ask *List all monitoring servers*.

If this matches Studio, production needs user-delegated Trimble auth (Option B long term).

### Option B — Server config (temporary)

In server `appsettings.json`:

```json
"TrimbleAccessToken": "paste-user-token-from-studio"
```

Tokens expire; refresh when chat breaks.

## 4. Agent Studio — tools and instructions

**Tools tab**

- Keep **`dbadash`**
- Remove **`get_server_status`** if listed (causes “running” / hangs in embed)

**Instructions** — ensure listing uses **exec**:

```
dbadash data calls must use "op": "exec".
"op": "list" returns action names only, not servers.

List monitoring servers:
{"op":"exec","action_name":"dbadash_list_instances","parameters_json":"{}"}
```

## Checklist

- [ ] `AgentStudioParityMode: true` on server
- [ ] Latest DLL deployed, app pool recycled
- [ ] Agent published in Studio
- [ ] `get_server_status` removed from agent tools in Studio
- [ ] Instructions include `op: exec` for `dbadash_list_instances`
- [ ] Test with `?trimbleToken=` or `TrimbleAccessToken` to match Studio auth

## Verify

Show Thinking for *List all monitoring servers*:

- **Good:** `dbadash` with `"op": "exec"`, `"action_name": "dbadash_list_instances"`
- **Bad:** `"op": "list"` only, or `get_server_status` spinning

`get_run_context` in parity mode should be **minimal** (not DBA Dash–injected fields).
