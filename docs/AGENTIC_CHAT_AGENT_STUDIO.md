# Agent Studio + DBA Dash embed — fix `dbadash` listing

## Problem

Calls with `"op": "list"` return **action names only**, not instance data.  
Calls with `"op": "exec"` return **real data**.

## Step 1 — Update Agent Studio system prompt

1. Open **Trimble Agentic Studio** (Stage).
2. Open agent **`8703e7ea-23b4-464b-a40c-d1901ee0c0ae`** (DBA Dash Web View).
3. Edit **Instructions** / **System prompt**.
4. **Append** the block below (or merge with your existing DBA instructions).
5. **Save** and **publish** the agent.

### Copy-paste block

```
## dbadash tool protocol

- To fetch monitoring data you MUST use `"op": "exec"`.
- `"op": "list"` only returns the catalog of action names (approximate_row_counts, dbadash_list_instances, …). It does NOT return SQL instances or metrics.
- Never call `op=list` when the user asks for instances, queries, waits, or any live data.

### List all monitoring instances

```json
{"op":"exec","action_name":"dbadash_list_instances","parameters_json":"{}"}
```

### Running queries for one instance (after you have an instance id from the list)

```json
{"op":"exec","action_name":"dbadash_running_queries_snapshot","parameters_json":"{\"instanceId\":1,\"limit\":10}"}
```

- Use exact action names with the `dbadash_` prefix.
- Do not use shorthand names (`list_instances`, `top_running_queries`).
- `parameters_json` must be valid JSON (`{}` not `{}`).
- Do not call `get_current_user`; user is in run context.
- Do not call `get_server_status`; use `dbadash` with `op=exec` (e.g. `dbadash_list_instances`) for servers/instances.
```

## Step 2 — Deploy latest WebView backend (IIS)

From your dev machine:

```bash
./scripts/build-publish.sh
```

Copy `dbadash-webview.zip` to the Windows server, expand, and run your IIS deploy script (e.g. `deploy-full-iis.ps1`). **Recycle the app pool.**

Ensure server `appsettings.json` **AgenticChat** section includes (merge with existing secrets):

```json
"AgenticChat": {
  "Enabled": true,
  "Environment": "Stage",
  "AgentId": "8703e7ea-23b4-464b-a40c-d1901ee0c0ae",
  "TrimbleChatScope": "agents",
  "TrimbleChatScopeCandidates": ["agents"],
  "TrimbleChatAudience": "",
  "SkipAgentsApiProbe": true,
  "RegisterApplicationWithAgent": true,
  "RegisterLocalRuntimeTools": false,
  "OnBeforeRunTimeoutMs": 30000
}
```

Do **not** set a long `AgentRunContextGuide` on the server unless you use the short protocol line from repo `appsettings.json`.

## Step 3 — Verify

1. Sign in to DBA Dash WebView.
2. Open Agentic Chat.
3. Ask: **List all monitoring instances**
4. Open **Show Thinking** → `dbadash` request must show **`"op": "exec"`**.
5. Response should look like tab-separated lines (`i`, `n`, server names), not a comma-separated list of action names.

### Golden-path test prompt

```
List monitoring instances using dbadash with op exec, action_name dbadash_list_instances, parameters_json {}. Then summarize the instance names.
```

## Optional — parity with Studio user token

If embed still differs from Studio, test with your Assist bearer token:

```
https://dbadash-nonprod.e-builder.net/agentic-chat-shell.html?trimbleToken=PASTE_TOKEN_HERE
```

Get the token from Agent Studio / Assist → browser DevTools → Network → Authorization header.
