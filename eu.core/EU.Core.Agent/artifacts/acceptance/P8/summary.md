# P8 controlled EU.Core.sln integration summary

Date: 2026-07-29
Status: **IMPLEMENTED_PENDING_MASTER_MERGE_AND_THREE_HOST_LIVE_QA**

## Delivered

- Migrated the accepted P7 package into the isolated
  `eu.core\EU.Core.Agent` container.
- Added Agent Api, Application, Runtime, Infrastructure, and Tests to the
  existing `EU.Core.sln` under the `EU.Core.Agent` Solution Folder.
- Preserved `EU.Core.Api`, `EU.Core.MCP.Api`, and `EU.Core.Agent.Api` as
  separate executable Hosts with separate project files and launch profiles.
- Preserved Agent package/build configuration inside the nested container, so
  existing EU Core dependency versions are not centrally overridden.
- Updated the architecture boundary test for the post-integration invariant:
  every Agent `ProjectReference` must resolve to one of the five projects inside
  the Agent container.
- Added no Controller, middleware, static asset, or runtime registration to
  `EU.Core.Api`.
- Added no `EuCoreAdapter` because no shared EU Core runtime dependency is
  required.
- Executed no database operation; SQLite remains the standalone development
  provider and external migration scripts remain manual.

## Verification

- Migrated Agent suite: **223 passed, 0 failed, 0 skipped**.
- Integrated `EU.Core.sln` Release build: **0 errors**.
- Existing target baseline also built with **0 errors** before migration.
- The target's existing warning/vulnerability backlog remains visible and was
  not rewritten as part of P8.
- Only one migrated source file differs from the accepted package:
  `SolutionArchitectureTests.cs`, for the deliberate post-integration boundary
  assertion.
- Configured ports do not conflict:
  `EU.Core.Api=8015`, `EU.Core.MCP.Api=8020`,
  `EU.Core.Agent.Api=62844`.
- Existing live process probe:
  Agent health returned `Healthy`/`single`; MCP endpoint returned HTTP 405 to a
  GET, confirming the MCP endpoint is listening and requires its protocol
  method.

## Remaining gate

- Merge `codex/p8-agent-integration` into the owner's `master` only after the
  owner chooses the integration action.
- Start all three Hosts from the final merged checkout and verify independent
  stop/restart behavior. `EU.Core.Api` was not started during this run because
  doing so may initialize the shared database, which the owner explicitly
  prohibited.
- Perform the approved ReadOnly MCP business call from Agent.Api after all
  three final-checkout Hosts are running.
