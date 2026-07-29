# EU.Core.Agent

Standalone Microsoft Agent Framework development workspace.

Current phase: **P7 Agent orchestration implemented, pending real-model
execution and browser QA** (PDF remains deferred).

The implementation is intentionally independent from `E:\EU\EU.Admin\eu.core`.
It aligns with that solution's `.NET 10` and SqlSugar baseline without adding
any `ProjectReference` to it.

## Current projects

- `spikes/EU.Core.Agent.Compatibility`: executable compatibility probes and reusable P0 code.
- `tests/EU.Core.Agent.CompatibilityTests`: P0 contract and security tests.
- `src/EU.Core.Agent.Application`: lifecycle, immutable versions,
  structured-output validation, Skill lifecycle/binding, queries, and V1
  configuration packages.
- `src/EU.Core.Agent.Infrastructure`: SQLite Agent/Skill persistence,
  controlled Skill files, and selectable in-memory repositories for tests.
- `src/EU.Core.Agent.Runtime`: Microsoft Agent Framework streaming execution
  and policy-controlled MCP tool adapters.
- `src/EU.Core.Agent.Api`: the independent HTTP Host, Agent REST API, health,
  and operator UI.
- `tests/EU.Core.Agent.Tests`: architecture, operations, Agent, API, and static
  UI tests.

The P2 Host persists Agent aggregates to a local SQLite database. It does not
connect to SQL Server, read `EUCORE_AGENT_SQLSERVER`, or execute the
operator-owned SQL Server migration placeholders.

## Commands

```powershell
dotnet restore .\EU.Core.Agent.Compatibility.slnx --locked-mode
dotnet test .\EU.Core.Agent.Compatibility.slnx --no-restore
dotnet build .\EU.Core.Agent.Compatibility.slnx --no-restore

dotnet restore .\EU.Core.Agent.sln
dotnet test .\EU.Core.Agent.sln --no-restore
dotnet build .\EU.Core.Agent.sln --no-restore
```

## Host configuration and operations

Use `.env.example` as the variable-name template. Visual Studio and the default
`dotnet run` launch profile enable the API Host's restricted local `.env`
bootstrap. It reads only non-secret Agent platform/model metadata and never
imports `AGENT_MODEL_API_KEY`, database settings, or arbitrary entries.
Process environment values take precedence. Outside that development launch
profile, export the required values into the process environment or set them
through User Secrets. The host requires the
`AgentPlatform__ServiceName`, `AgentPlatform__ModelEndpoint`, and
`AgentPlatform__ModelCredentialAlias` settings. The alias must use the
unambiguous bounded `alias:<safe-name>` format, for example
`alias:development-agent-model`; it is an identifier, not a credential. Raw
credentials, authorization values, passwords, tokens, and connection strings
are rejected from host options and checked-in templates.
A resolver for environment variables or User Secrets will be added in a later
phase.

For local development, existing `AGENT_MODEL_ENDPOINT` and
`AGENT_MODEL_DEFAULT_ID` values are mapped to the corresponding platform
Endpoint and first public model profile; safe defaults are used for the local
service name and credential alias. P5 resolves `AGENT_MODEL_API_KEY` or the
alias-specific `AGENT_MODEL_CREDENTIAL_<NORMALIZED_ALIAS>` directly from the
process environment or nearest ignored `.env`; the value is not copied into
Host options or returned by an API.

The P2 editor also requires one or more public model-profile identifiers, for
example `AgentControl__ModelProfileIds__0=qwen-safe`. Add `__1`, `__2`, and
so on for more IDs. These values are a public allowlist, not API keys.
Credential/path/URL-shaped values fail Host startup and are never exposed by
the capabilities API.

The host exposes:

- `GET /` for the Agent operator UI;
- `GET /api/platform/service` for safe service metadata;
- `GET /api/platform/capabilities` for safe UI capability facts;
- `GET /health` for process health, including `replicaMode: single`.
- `/api/agents` for list/search/create, Draft, publish, status, detail, and V1
  import/export operations.
- `/api/skills` for list/search/create, metadata, controlled Draft files, and
  immutable publish operations.
- `GET /api/skill-versions` for the published-version binding catalog.
- `/api/mcp/servers` for MCP Server configuration, synchronization and tool
  risk classification.
- `GET /api/mcp/tool-versions` for classified current tool versions available
  to Agent Drafts.
- `POST /api/agents/{agentId}/runs` for bounded SSE execution of the latest
  published version, and `GET` on the same route for metadata-only run audit.

SQLite is the default Agent repository:

```text
AgentStorage__Provider=Sqlite
AgentStorage__DatabasePath=data/eu-core-agent.db
AgentStorage__SkillRootPath=agent-data/skills
```

Relative paths resolve from the API Host content root. The Host creates the
parent directory and idempotent local SQLite schema on startup. Agent Code is
unique, and updates use `logical_revision` as an atomic compare-and-swap guard.
Set `AgentStorage__Provider=InMemory` only for isolated tests; that mode clears
state on restart.

Health does not declare multi-instance readiness. The operator-owned SQL Server
scripts stay manually executed. OpenAPI is mapped automatically
only in Development, or when the deliberate non-secret
`AgentPlatform__ExposeOpenApi=true` setting is supplied.

Agent, Skill, and MCP control-plane metadata survive Host restarts when SQLite
is used. Skill Draft
files live under the configured controlled root; published files are hashed and
read-only. `scripts/` may be stored but has no execution endpoint. P4 MCP
supports Streamable HTTP, SSE, and explicitly enabled/allowlisted stdio
discovery. HTTP and HTTPS origins are denied unless `AgentMcp__AllowedHosts__*` and
`AgentMcp__AllowedPorts__*` allow them; DNS is revalidated and the selected
address is pinned for each transport connection. Allowlisted loopback and
private-network endpoints are supported for local and intranet MCP Servers.
Credential values are
never stored: records contain only optional `alias:` identifiers. HTTP aliases
resolve at call time from process environment names shaped as
`AGENT_MCP_CREDENTIAL_<NORMALIZED_ALIAS>` and are sent only as Bearer
authorization on the allowlisted origin; the restricted `.env` loader does not
import these values. Stdio credential injection is deliberately unsupported. Discovered
tools must be classified before Agent authorization; Agent publish snapshots
freeze immutable tool-version IDs. P5 exposes MCP invocation only through an
enabled Agent's immutable published authorization snapshot; it has no direct
MCP tool-run endpoint. Only ReadOnly tools execute automatically, while
Unknown, Mutating, and HighRisk calls are blocked before network access.
Knowledge and manual orchestration surfaces are enabled; schedules remain
disabled for P8 so the Host cannot start unattended Agent work. There is no
Agent, Skill, or MCP Server delete endpoint.

The P0 compatibility test harness is distinct from the API host. Its SQL
Server gate is opt-in and reads `EUCORE_AGENT_SQLSERVER` only from the current
process environment. Its real model smoke tests (and only those tests)
automatically load the nearest `.env` file (starting from the working directory
or test output directory) and use the endpoint, credential, and default-model
P0 environment settings (`AGENT_MODEL_NAME` remains a legacy alias). Existing
process environment variables take precedence over `.env`; only
`AGENT_MODEL_*` entries are imported, so unrelated application and database
configuration remains explicit. Set
`AGENT_MODEL_SUPPORTS_IMAGES=true` and/or
`AGENT_MODEL_SUPPORTS_PDF=true` only when the selected model claims those
capabilities; each flag enables its corresponding live gate. Real values must
only be written to the ignored local `.env`; do not put them in `.env.example`,
source control, test output, or acceptance artifacts.

The P0 input adapter accepts only controlled inline attachments up to 20 MiB.
It enforces an allowlisted MIME type, matching file extension and content
signature before model execution; remote `UriContent` is not accepted.

P2 evidence is under `artifacts/acceptance/P2`, P3 evidence is under
`artifacts/acceptance/P3`, P4 evidence is under `artifacts/acceptance/P4`, and
P5 evidence is under `artifacts/acceptance/P5`, and P6 evidence is under
`artifacts/acceptance/P6`, and P7 evidence is under `artifacts/acceptance/P7`.
P0 compatibility remains separate from local SQLite
persistence; its SQL Server gates are still opt-in, and the real-provider PDF
gate remains explicitly deferred.

P6 supports UTF-8 plain-text and Markdown knowledge sources, bounded
deterministic chunking, Chinese/alphanumeric lexical retrieval, Agent knowledge
authorization, immutable published revision bindings, runtime evidence
injection, and streamed source citations. Retrieved text is delimited as
untrusted reference data. The local Host uses SQLite; the SQL Server 0006
migration is a documentation-only placeholder and is never executed.

P7 supports bounded directed acyclic Agent flows, conditional edges, controlled
input mapping, per-node retry and timeout, immutable Agent-version bindings,
manual background execution, cancellation, and restart recovery. SQLite now
permanently stores the orchestration input/final output, every node-attempt
input/Agent output, and every MCP call's arguments/raw result in normalized
tables. Run-list records remain compact hashes/counts; full content is loaded
only by `GET /api/orchestrations/{id}/runs/{runId}/details`.

After a terminal run, expand “节点工具调用明细” in the operator timeline to
inspect each Attempt and MCP call. JSON is pretty-printed and all payloads are
rendered through `textContent`. Credential-shaped JSON fields are recursively
redacted before persistence. Payloads are never silently truncated: exceeding
the configured node/tool/final-output limits fails the run with
`ORCHESTRATION_PAYLOAD_LIMIT_EXCEEDED`. Product policy currently keeps these
records permanently, so the SQLite file must be protected as sensitive
operator data.

`database/migrations/0008_orchestration_execution_details.sql` documents the
normalized SQLite-to-MySQL/SQL Server mapping and manual verification sequence.
It contains no executable external-database operation. Provider implementation
and migration execution remain owner-controlled. Scheduling remains explicitly
deferred to P8.
