<div align="center">

<img src="docs/logo.svg" alt="DBA Dash WebView" width="120" />

# DBA Dash WebView

**A modern web dashboard for SQL Server fleet monitoring**

*Browser-based companion to [DBA Dash](https://github.com/trimble-oss/dba-dash) — monitor hundreds of SQL Servers from any device.*

[![Build](https://github.com/e-buildernoc/DBADash-WebView/actions/workflows/build.yml/badge.svg)](https://github.com/e-buildernoc/DBADash-WebView/actions/workflows/build.yml)
[![MIT License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-19-61dafb.svg)](https://react.dev/)
[![DBA Dash](https://img.shields.io/badge/Powered%20by-DBA%20Dash-green.svg)](https://dbadash.com)

[Features](#-features) · [Screenshots](#-screenshots) · [Quick Start](#-quick-start) · [Deployment](#-iis-deployment) · [Configuration](#-configuration) · [API Reference](#-api-reference) · [Roadmap](#-roadmap) · [Contributing](#-contributing)

---

**DBA Dash** is an outstanding open-source SQL Server monitoring tool by [Trimble](https://github.com/trimble-oss/dba-dash). **DBA Dash WebView** gives it a web UI — access your fleet's health from any browser, any device, anywhere.

</div>

---

## The Problem

DBA Dash has a powerful Windows GUI — but in modern IT environments, that's not always enough:

| Challenge | How WebView Helps |
|-----------|------------------|
| DBA Dash GUI is Windows-only | WebView runs in **any browser** — Mac, Linux, iPad, phone |
| Can't share dashboards with management | One URL, everyone sees live data — **no install required** |
| VPN required to check server health | Deploy on an internal IIS, access from anywhere on your network |
| IT managers need high-level overviews | **Management dashboards** with RPO analysis, license costs, fleet KPIs |
| Setting up monitoring views takes time | **46 pre-built pages** for common DBA workflows |

**Zero impact on your existing setup** — WebView reads from the same `DBADashDB` your collectors already write to. No additional agents, no schema changes, no configuration needed on monitored servers.

---

## ✨ Features

### 📋 Summary Dashboard (NEW)
The first thing you see — a faithful recreation of DBA Dash's original Summary tab. Status matrix showing OK / Warning / Critical / N/A / Acknowledged counts for every health check across your fleet. One glance tells you where to focus.

Uses `dbo.Summary_Get` stored procedure with correct `DBADashStatusEnum` mapping:
- **1 = Critical** (red), **2 = Warning** (yellow), **3 = N/A** (gray), **4 = OK** (green), **5 = Acknowledged** (blue)

Health checks tracked: Backup FULL/DIFF/LOG, Drive Space, File Space, Log Space, Agent Jobs, Availability Groups, Corruption, Last Good CheckDB, Memory Dump, Snapshot Age, Instance Uptime, Agent Running, DB Mail, Query Store, SQL Agent Alerts, % Max Size, Collection Errors, Database State, Identity Columns, Log Shipping, Custom Checks, Mirroring, Elastic Pool Storage.

### 🖥️ Tabbed Dashboard
The main dashboard mirrors the DBA Dash GUI with **5 tabs** — exactly like the original:

1. **Summary** — status matrix with instance counts per health check
2. **Alerts** — unified error feed (collection errors + failed jobs)
3. **Performance Summary** — fleet-wide CPU, waits, IO latency table (sorted by Max CPU)
4. **Slow Queries** — Extended Events slow query data
5. **Running Queries** — live executing queries with blocking detection

All tabs auto-refresh every 30 seconds with countdown indicator.

### 🖥️ SQL Monitor Dashboard
Real-time fleet overview inspired by Redgate SQL Monitor — card-based grid showing all instances with health indicators, CPU usage, and status at a glance. Alert sidebar with live error feed. Click any card to drill into instance details.

### 🌳 DBA Dash-Style Navigation
Full instance tree sidebar — grouped by SQL Server version (2025, 2022, 2019…), each instance expandable with categories: Configuration, HA/DR, Storage, Databases, Backups, Jobs, Reports. Click any node → filtered view for that server only.

### 🚦 Backup Ampel Report
Traffic-light backup compliance across the entire fleet:

- **Per-instance ampel status** — GREEN (Full ≤24h & Log ≤1h), YELLOW (Full ≤48h & Log ≤2h), RED (everything else)
- **AlwaysOn-aware** — AG secondaries correctly excluded from backup evaluation (backups run on preferred replica)
- **Simple Recovery handling** — databases without log backups show N/A, not RED
- **RPO analysis** — average and worst-case RPO across the fleet, distribution charts
- **Expandable per-database details** — drill into any instance to see individual DB backup status, AG role, TDE, recovery model
- **Interactive pie charts** — click to filter by status

### 🔄 AlwaysOn Availability Groups
Fleet-wide AG overview with cluster visualization:

- **Cluster cards** with server topology — Primary/Secondary roles, CPU bars, RAM, availability mode
- **Database sync state** — SYNCHRONIZED, SYNCHRONIZING, NOT SYNCHRONIZING with health indicators
- **Lag monitoring** — log send queue, redo queue, send/redo rates per database
- **Search** — filter across server names, AG names, and database names
- **Per-instance HA/DR view** — click through to individual server AG details

### 📈 Instance Detail Pages
Comprehensive per-instance view with tabbed navigation:

- **Performance** (default tab) — CPU chart (24h), wait type analysis, CPU KPIs
- **Backups** — per-database backup status grouped by DB with Full/Diff/Log age, AG-aware
- **Jobs** — filterable by status (All/Failed/Success), duration, messages
- **Databases** — state, recovery model, AG role, sync state, last DBCC
- **Drives** — visual capacity cards with usage percentage and color coding (instance-filtered when navigating from tree)

### 🚨 Alerts & Errors
Unified alert feed combining Collection Errors and Failed Jobs:

- **Severity filtering** — Critical, Warning, Info with KPI counters
- **Type filtering** — Collection errors vs failed jobs
- **Detail panel** — click any alert for full error message, context, server link
- **Server breakdown** — which instances generate the most alerts
- **Auto-refresh 30s** — newest alerts always on top

### 📋 Management Reporting
Purpose-built reports for IT managers:

- **Fleet Statistics** — CPU distribution, top consumers, RAM/storage allocation, version/edition pie charts
- **License Overview** — SQL Server edition distribution, core & RAM totals, end-of-support timeline
- **Underutilized Servers** — instances averaging <5% CPU, candidates for consolidation or downsizing
- **Backup & Recovery Overview** — RPO compliance, recovery time estimates, expandable instance cards

### 🔍 Performance Deep-Dive
- **Running Queries** — live executing queries with blocking detection
- **Blocking Analysis** — tree view of blocking chains, root blockers highlighted
- **Slow Queries** — Extended Events data with duration/DB/application filters
- **Wait Statistics** — stacked area chart of wait types over time
- **Memory** — buffer pool, PLE trends, memory clerk breakdown
- **IO Performance** — read/write latency charts, IOPS, throughput per file
- **Object Execution Stats** — stored procedure and function performance
- **Performance Counters** — custom counter monitoring with trend charts
- **Query Store** — top resource consumers from Query Store data

### 📅 Daily Health Checks
- **Backup Status** — Full/Diff/Log backup age per database, RPO compliance
- **Agent Jobs** — job history with Gantt-style timeline visualization
- **Drive Space** — capacity monitoring with usage percentage and color thresholds
- **Database Space** — file-level space tracking with growth analysis
- **TempDB** — file configuration and usage monitoring

### 🔬 Tracking & Compliance
- **Configuration Tracking** — detect sp_configure changes with before/after diff
- **SQL Patching** — version distribution across fleet, patch history timeline
- **Schema Changes** — DDL change history timeline
- **Identity Columns** — usage percentage with threshold alerts

### ⚙️ Administration
- **Configurable Thresholds** — define warning/critical levels per metric
- **Active Directory Authentication** — LDAP integration with group-based roles
- **Local + AD Auth** — hardcoded admin fallback when AD is down
- **Server Management** — view monitored instances and connection details
- **Groups & Tags** — organize instances for filtering
- **Users & RBAC** — Admin / Operator / Viewer roles
- **Data Retention** — configure cleanup per data category

### 🎨 User Experience
- **Command Palette** (Ctrl+K) — instant fuzzy search across instances, databases, jobs
- **Auto-Refresh** — 30-second intervals with countdown indicator, diff-based updates (no full page remount)
- **Dark Theme** — glassmorphism design, optimized for NOC/SOC wall displays
- **Framer Motion** — smooth page transitions and animated list items
- **Instance-Aware Navigation** — tree links filter to selected instance (drives, backups, config)
- **Responsive** — collapsible sidebar, works on tablets
- **Fast** — React 19 + Vite, sub-second page transitions

---

## 📸 Screenshots

> *Screenshots from a production environment with 200+ SQL Server instances.*

![Summary Dashboard](docs/screenshots/summary.png)
*Summary — status matrix with OK/Warning/Critical/N/A counts per health check*

![SQL Monitor Dashboard](docs/screenshots/monitor.png)
*SQL Monitor — card-based fleet overview with real-time health indicators*

![Backup Ampel](docs/screenshots/backup-ampel.png)
*Backup Ampel — traffic-light compliance report with AG-aware logic*

![AlwaysOn AGs](docs/screenshots/availability-groups.png)
*Availability Groups — cluster topology with sync state and lag monitoring*

![Instance Detail](docs/screenshots/instance-detail.png)
*Instance Detail — Performance tab with CPU chart and wait analysis*

![Alerts](docs/screenshots/alerts.png)
*Alerts — unified error feed with severity filtering and server breakdown*

---

## 🚀 Quick Start

### Prerequisites

| Requirement | Version |
|-------------|---------|
| [DBA Dash](https://github.com/trimble-oss/dba-dash) | Any (populated DBADashDB required) |
| [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) | 8.0+ (Required for building from source) |
| [Node.js](https://nodejs.org/) | v18+ (Required for frontend development) |
| SQL Server | 2012+ |

### Option A: For Users (Pre-Compiled Release)

1. Download the latest release from [Releases](https://github.com/e-buildernoc/DBADash-WebView/releases)
2. Extract the ZIP
3. Edit `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DBADashDB": "Server=YOUR_SQL_SERVER;Database=DBADashDB;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=true;"
  }
}

```

4. Deploy to IIS (see below) or run standalone:

```bash
dotnet DBADashWebView.dll
# Open http://localhost:5000

```

5. Login with `admin` / `admin` *(change this in production!)*

### Option B: For Developers (Build from Source)

Because compiled binaries are not tracked in version control, you must build the project locally using our automation scripts after cloning.

1. **Clone the repository:**

```bash
git clone [https://github.com/e-buildernoc/DBADash-WebView.git](https://github.com/e-buildernoc/DBADash-WebView.git)
cd DBADash-WebView

```

2. **Install Dependencies:**
Make sure all node packages and .NET packages are pulled down before initiating a build:

```bash
cd frontend && npm install && cd ../backend && dotnet restore && cd ..

```

3. **Execute Build & Hot-Deploy Automation Scripts:**
Run the custom project scripts located in the scripts folder to compile and structure the platform artifacts:

```bash
# Ensure scripts have execute permissions
chmod +x ./scripts/*.sh

# Compile backend and package the frontend single page app assets
./scripts/build-publish.sh

# Prepare the chat orchestration plugin hot-deploy matrix
./scripts/prepare-chat-hot-deploy.sh

```

4. **Run the Application locally:**

```bash
# Inside the backend directory
cd backend
dotnet run

```

*(The backend Minimal API will serve the frontend assets on `http://localhost:5000`. Alternatively, run `npm run dev` in the frontend folder for hot-module reloading).*

---

## 🖥️ IIS Deployment

### 1. Install the ASP.NET Core Hosting Bundle

Download from [Microsoft](https://dotnet.microsoft.com/download/dotnet/8.0) → **Hosting Bundle** (not just Runtime).

```powershell
iisreset  # Required after installing the Hosting Bundle

```

### 2. Create the IIS Site

```powershell
# Extract release
Expand-Archive -Path dbadash-webview.zip -DestinationPath C:\inetpub\dbadash

# Create App Pool (No Managed Code)
Import-Module WebAdministration
New-WebAppPool -Name "DBADashWebView"
Set-ItemProperty "IIS:\AppPools\DBADashWebView" -Name "managedRuntimeVersion" -Value ""

# Create Website
New-Website -Name "DBADashWebView" -PhysicalPath "C:\inetpub\dbadash" `
            -ApplicationPool "DBADashWebView" -Port 8080

# Grant read permissions
icacls "C:\inetpub\dbadash" /grant "IIS AppPool\DBADashWebView:(OI)(CI)R" /T

```

### 3. Configure Connection String

Edit `C:\inetpub\dbadash\appsettings.json` — set your DBADashDB server, credentials, and add `Encrypt=false` if using SQL authentication without TLS.

### 4. Browse to `http://your-server:8080`

### Troubleshooting

| Symptom | Fix |
| --- | --- |
| 502.5 / 500.30 | Install the Hosting Bundle, then `iisreset` |
| Blank page, no errors | Check connection string — server reachable? Correct DB name? |
| No instances showing | SQL user needs `db_datareader` on DBADashDB |
| "HTTP Error 500.19" | Hosting Bundle not installed or `web.config` invalid |
| Login fails | Default credentials: `admin` / `admin` |
| CORS errors | Deploy frontend and backend together (same origin) |

Enable detailed logging:

```xml
<aspNetCore stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" ... />

```

---

## ⚙️ Configuration

### SQL Server Permissions

WebView needs **read-only** access to DBADashDB:

```sql
USE DBADashDB;
CREATE LOGIN [dbadashweb] WITH PASSWORD = 'YourSecurePassword';
CREATE USER [dbadashweb] FOR LOGIN [dbadashweb];
ALTER ROLE db_datareader ADD MEMBER [dbadashweb];
GRANT EXECUTE ON SCHEMA::dbo TO [dbadashweb];

```

### Active Directory Authentication

Configure via **Settings → Users → LDAP tab**, or directly edit `config/ad-config.json`:

```json
{
  "Enabled": true,
  "Server": "ldap://dc01.corp.local",
  "BaseDN": "DC=corp,DC=local",
  "BindUser": "CN=svc-dbadash,OU=Service,DC=corp,DC=local",
  "AdminGroup": "CN=DBA-Admins,OU=Groups,DC=corp,DC=local",
  "AllowLocalFallback": true
}

```

When `AllowLocalFallback` is true, the built-in `admin` account still works when AD is unreachable.

### Dashboard Thresholds

Configure via **Settings → Thresholds**. Define warning and critical levels per metric (CPU %, IO latency, wait ms, etc.). **Cells remain neutral/uncolored** until you explicitly set thresholds — no surprise colors out of the box.

---

## 📡 API Reference

All endpoints require JWT authentication via `Authorization: Bearer <token>` header.

* **`POST /api/auth/login`**
Authenticates security credentials against configured Local/Active Directory directories, returning a signed JWT authorization string.
* **`GET /api/health`**
Public, unauthenticated heartbeat indicator validating that the ASP.NET routing engine and database thread-pools are active.

* **`GET /api/dashboard/summary`**
Pipes processed metric arrays out of `dbo.Summary_Get` to feed the main OK/Warning/Critical fleet matrix.
* **`GET /api/dashboard/stats`**
Exposes high-level server metrics: overall system alerts, database size rankings, and top processor consumers.
* **`GET /api/dashboard/performance-summary`**
Populates tabular rows comparing current CPU usage, wait counts, and I/O latency metrics across the fleet.
* **`GET /api/dashboard/monitor`**
Powers the Redgate-style monitoring grid view, supplying live health scores and server state cards.
* **`GET /api/tree`**
Compiles the expandable navigation sidebar tree layout, grouping instances cleanly by SQL version.
* **`GET /api/instances`**
Returns a global index listing of every monitored database engine instance along with its configuration attributes.
* **`GET /api/instances/{id}`**
Fetches the targeted overview metrics and state attributes for a single designated server instance.

These endpoints fetch specialized data context filterable by a specific managed instance ID (`{id}`).

* **`GET /api/instances/{id}/cpu`**
Retrieves granular CPU usage logs, historical processor utilization breakdowns, and operating system scheduling metrics for the designated target server.
* **`GET /api/instances/{id}/waits`**
Returns aggregated SQL Server instance-level wait statistics, mapped by wait classification types, to facilitate diagnostic performance isolation.
* **`GET /api/instances/{id}/drives`**
Fetches logical block volume configurations and drive storage capacities. Maps threshold alerts based on remaining storage percentages.
* **`GET /api/instances/{id}/databases`**
Returns catalog metadata regarding all tracked database configurations on the host instance, describing state flags, compatibility levels, and structural definitions.
* **`GET /api/instances/{id}/backups`**
Provides database backup compliance history, listing timestamps for the latest Full, Differential, and Transaction Log execution states.
* **`GET /api/instances/{id}/jobs`**
Extracts SQL Server Agent engine telemetry, parsing job definitions, history success/failure tables, and execution step error logs.
* **`GET /api/instances/{id}/queries`**
Summarizes high-impact query footprints or execution frequencies mapped to this instance token.
* **`GET /api/instances/{id}/hadr`**
Provides replication node telemetry detailing AlwaysOn High Availability and Disaster Recovery alignments, preferred routing topologies, and cluster definitions.

Endpoints configured to analyze diagnostic wait metrics, performance counters, or runtime profiles.

* **`GET /api/performance/running-queries?instanceId=`**
Exposes executing session profiles on the engine thread pool, mapping wait patterns, execution context tokens, and command text structures.
* **`GET /api/performance/blocking?instanceId=`**
Constructs active engine transaction block graphs, isolating head blocker nodes, root lock contentions, and resource chain graphs.
* **`GET /api/performance/slow-queries?instanceId=&hours=24`**
Queries Extended Events telemetry collectors to compile queries exceeding engine speed thresholds inside the designated window parameters.
* **`GET /api/performance/memory?instanceId=`**
Maps instance memory allocations, caching layer distribution tables, Buffer Pool capacities, and Page Life Expectancy (PLE) status metrics.
* **`GET /api/performance/io?instanceId=`**
Documents drive and database physical file file-stall latencies, tracking read/write processing rates and IOPS limitations.
* **`GET /api/performance/exec-stats?instanceId=&hours=24`**
Evaluates execution plan efficiency records, highlighting execution count histograms and duration metrics for compiled objects.
* **`GET /api/performance/waits-timeline?instanceId=&hours=24`**
Populates time-series data visualization arrays for tracked server wait components inside the monitoring window.
* **`GET /api/performance/counters?instanceId=&hours=24`**
Extracts custom operating system and engine counter tables (e.g., User Connections, Batch Requests/sec) mapped to user threshold definitions.
* **`GET /api/performance/query-store?instanceId=`**
Connects to native engine Query Store catalog data tables to summarize highest consumer traces across the server environment.

Enables compliance tracking, environment drifts, identity usage, space monitoring, and patching history tracking.

* **`GET /api/monitoring/job-timeline?instanceId=&hours=24`**
Formats historical SQL Agent workflow schedules into Gantt-chart timeline coordinate streams for tracking concurrent jobs and overlapping paths.
* **`GET /api/monitoring/configuration?instanceId=`**
Dumps localized engine infrastructure system variables retrieved directly from `sys.configurations` registries.
* **`GET /api/monitoring/configuration/changes?instanceId=&days=30`**
Provides systemic diff audit logs recording explicit configuration updates or adjustments inside the designated tracking interval.
* **`GET /api/monitoring/patching`**
Aggregates estate-wide build versions, patch states, release update indicators, and cumulative updates across the managed topology.
* **`GET /api/monitoring/schema-changes?instanceId=&days=30`**
Parses DDL structural change history tables to map configuration updates, object dropped notifications, or index modifications.
* **`GET /api/monitoring/identity-columns?instanceId=`**
Validates structural range bounds across tables, highlighting columns approaching field maximums to prevent identity allocation exhaustion errors.
* **`GET /api/monitoring/tempdb?instanceId=`**
Pipes runtime operational health charts for the TempDB structures, parsing allocation spaces, tracking version stores, and scanning object contentions.
* **`GET /api/monitoring/db-space?instanceId=`**
Exposes logical database file metrics, providing space tracking calculations and auto-growth threshold indicators.

Provides estate-wide audits, unified error metrics, licensing overviews, underutilized resource reporting, and advanced backup compliance tracking.

* **`GET /api/alerts/recent`**
Consolidates multi-instance faults across the entire fleet, tracking collectors' telemetry faults and failed job steps over the past 48 hours.
* **`GET /api/jobs/recent`**
Aggregates the execution history of recent automated tasks across the ecosystem, feeding into unified operational tracking grids.
* **`GET /api/jobs/failures`**
Isolates active job exceptions and failures, compiling exact step crash reports, exit codes, and runtime engine messages.
* **`GET /api/drives`**
Gathers storage information globally across all managed drives to identify critical capacity shortages and usage velocity alerts.
* **`GET /api/backups/estate`**
Evaluates global ecosystem backup profiles to flag systems lacking protection plans, schedule deviations, or policy gaps.
* **`GET /api/backups/management`**
Exposes compliance analytics for IT managers, showing average vs. worst-case RPO statistics and recovery projection timelines.
* **`GET /api/availability-groups`**
Maps cross-instance AlwaysOn networks, illustrating active primary/secondary topologies, replication metrics, and cluster roles.
* **`GET /api/availability-groups/{id}`**
Drills down into node layouts, listener routing configurations, and replication delay vectors matching the specific instance identification token.
* **`GET /api/reports/licenses`**
Audits core allocations, software distribution topologies, and license profiles across environments. Includes life-cycle markers for end-of-support timelines.
* **`GET /api/reports/underutilized`**
Analyzes systemic workloads to identify servers averaging under 5% CPU capacity for infrastructure consolidation or downsizing candidates.
* **`GET /api/reports/fleet-stats`**
Compiles summary telemetry matrices describing infrastructure distributions, storage maps, and version distribution charts.
* **`GET /api/reports/backup-ampel`**
Feeds the traffic-light compliance engine, calculating green/yellow/red status trees while properly evaluating AlwaysOn secondary nodes and Simple recovery rules.

Manages identity synchronization rules, portal metric alerting thresholds, loopback execution test paths, and unparsed procedure traces.

* **`GET /api/settings/ad`**
Retrieves configured LDAP connection strings, security base directories, and Active Directory group-based role mappings.
* **`POST /api/settings/ad`**
Saves or modifies secure configuration arrays for Active Directory synchronization workflows and fallback admin settings.
* **`POST /api/settings/ad/test`**
Triggers a secure real-time authentication test loop to validate connections to the configured LDAP directory services.
* **`GET /api/settings/thresholds`**
Loads current global parameters for dashboard color thresholds, setting the points where alerts shift to caution or danger states.
* **`POST /api/settings/thresholds`**
Commits customized warning and critical ranges for systemic monitoring metrics (e.g., CPU maps, stall latency thresholds).
* **`GET /api/debug/summary/{id}`**
Dumps the raw, unparsed JSON output directly from `dbo.Summary_Get` for an instance token to speed up developer troubleshooting and trace analysis.

---

## 🏗️ Architecture

```
┌──────────────────┐       ┌──────────────────┐       ┌─────────────────┐
│                  │       │                  │       │                 │
│  Browser         │──────▶│  ASP.NET Core 8  │──────▶│   DBADashDB     │
│  (React SPA)     │  JWT  │  (Minimal API)   │  SQL  │   (SQL Server)  │
│                  │       │                  │       │                 │
└──────────────────┘       └──────────────────┘       └────────┬────────┘
                                  │                            │
                             IIS / Kestrel              DBA Dash Agents
                             Static files               (your collectors)
                             Read-only queries                 │
                                                     ┌────────┴────────┐
                                                     │  Your SQL       │
                                                     │  Server Fleet   │
                                                     │  (10–1000+)     │
                                                     └─────────────────┘

```

| Layer | Technology |
| --- | --- |
| **Frontend** | React 19, TypeScript, Vite, Tailwind CSS 4, Recharts, Framer Motion, Lucide Icons |
| **Backend** | ASP.NET Core 8 Minimal API, Microsoft.Data.SqlClient |
| **Auth** | JWT tokens + optional LDAP/Active Directory |
| **Deployment** | IIS with ASP.NET Core Hosting Module |
| **CI/CD** | GitHub Actions → ZIP artifact → GitHub Release |

### Page Count: 46

| Category | Pages |
| --- | --- |
| Dashboard & Navigation | 5 (Summary, Tabbed Dashboard, SQL Monitor, Performance Summary, Tree) |
| Performance Monitoring | 10 (Running Queries, Blocking, Slow Queries, Waits, Memory, IO, Exec Stats, Counters, Query Store, Analysis) |
| Daily Health Checks | 6 (Backups, Jobs, Job Timeline, Drives, DB Space, TempDB) |
| Estate Views | 6 (Backup Ampel, Estate Backups, Estate Disk, Alerts, AGs, Instances) |
| Management Reporting | 3 (Fleet Stats, License Overview, Underutilized) |
| Tracking & Compliance | 4 (Configuration, Patching, Schema Changes, Identity Columns) |
| Administration | 6 (Thresholds, Alert Settings, Servers, Groups, Users, Retention) |
| Other | 6 (Instance Detail, Database Detail, Login, About, Reports Hub, Debug) |
| **Total** | **46** |

### DBA Dash Status Enum

WebView correctly maps the `DBADashStatusEnum` values used throughout `dbo.Summary_Get` and all status columns:

| Value | Enum | Color | Meaning |
| --- | --- | --- | --- |
| 1 | Critical | 🔴 Red | Immediate attention required |
| 2 | Warning | 🟡 Yellow | Threshold exceeded, review needed |
| 3 | N/A | ⚪ Gray | Check not applicable / not configured |
| 4 | OK | 🟢 Green | All good |
| 5 | Acknowledged | 🔵 Blue | Known issue, acknowledged by admin |

> **Note:** This is the opposite of what you might expect (1=worst, not best). Verified against [`DBADashGUI/DBAChecksStatus.cs`](https://www.google.com/search?q=https://github.com/trimble-oss/dba-dash/blob/main/DBADashGUI/DBAChecksStatus.cs).

---

## 🗺️ Roadmap

* [ ] Scheduled PDF reports via email
* [ ] Multi-tenant support (multiple DBADashDB repositories)
* [ ] Custom dashboard layouts (drag & drop widgets)
* [ ] Webhook / Teams / Slack notifications
* [ ] Dark/Light theme toggle
* [ ] SignalR real-time push updates (replace polling)
* [ ] Export to CSV/Excel from any table
* [ ] Mobile-optimized views for on-call DBAs
* [ ] Integration with Grafana / Prometheus exporters
* [ ] Automated health score per instance

---

## 🤝 Contributing

Contributions welcome! Fork the repo, create a feature branch, and submit a PR.

```bash
git checkout -b feature/my-feature
git commit -m 'feat: add my feature'
git push origin feature/my-feature

```

Please ensure `npm run build` and `dotnet build` pass before submitting.

---

## 🙏 Acknowledgements

**[DBA Dash](https://github.com/trimble-oss/dba-dash)** by [David Wiseman](https://github.com/DavidWiseman) / [Trimble](https://github.com/trimble-oss) — the outstanding open-source SQL Server monitoring tool that provides all the data WebView visualizes. Licensed under Apache 2.0.

If you're not using DBA Dash yet, [check it out](https://dbadash.com) — it's one of the best SQL Server monitoring tools available, period.

DBA Dash WebView is an **independent project** that provides a web frontend for DBA Dash data. It is not affiliated with or endorsed by Trimble or the DBA Dash project.

---

## 📄 License

[MIT](https://www.google.com/search?q=LICENSE) — DBA Dash WebView

[Apache 2.0](https://github.com/trimble-oss/dba-dash/blob/main/LICENSE) — DBA Dash

---

**Built by [e-buildernoc**](https://github.com/e-buildernoc)

*If this project helps you, give it a ⭐!*