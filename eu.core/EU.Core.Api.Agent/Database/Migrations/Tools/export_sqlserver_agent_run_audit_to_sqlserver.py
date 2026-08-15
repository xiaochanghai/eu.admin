#!/usr/bin/env python3
"""Generate normalized Agent run audit SQL from current SQL Server rows."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from export_sqlite_to_sqlserver import sql_server_text


RUN_STATUS_BY_ORDINAL = {
    0: "Running",
    1: "WaitingForApproval",
    2: "Completed",
    3: "Failed",
    4: "Cancelled",
}
EVENT_KIND_BY_ORDINAL = {
    0: "Started",
    1: "SkillStarted",
    2: "KnowledgeRetrieved",
    3: "Delta",
    4: "Citation",
    5: "ToolStarted",
    6: "ToolSucceeded",
    7: "ToolBlocked",
    8: "ToolFailed",
    9: "ApprovalRequired",
    10: "Completed",
    11: "Failed",
    12: "Cancelled",
}
RISK_BY_ORDINAL = {0: "Unknown", 1: "ReadOnly", 2: "Mutating", 3: "HighRisk"}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("output", type=Path, help="Destination Agent run audit normalization SQL")
    parser.add_argument(
        "--connection-env",
        default="AGENT_RUN_AUDIT_MIGRATION_SQLSERVER_ODBC",
        help="Environment variable containing an ODBC SQL Server connection string",
    )
    return parser.parse_args()


def enum_name(value: Any, names: dict[int, str], field: str) -> str:
    if isinstance(value, bool):
        raise ValueError(f"{field} {value!r} is invalid")
    if isinstance(value, int) and value in names:
        return names[value]
    text = str(value).strip()
    if text.lstrip("-").isdigit() and int(text) in names:
        return names[int(text)]
    for name in names.values():
        if text.casefold() == name.casefold():
            return name
    raise ValueError(f"{field} {value!r} is invalid")


def guid(value: Any, field: str) -> str:
    try:
        return str(uuid.UUID(str(value)))
    except (ValueError, AttributeError) as error:
        raise ValueError(f"{field} {value!r} is not a GUID") from error


def bounded(value: Any, maximum: int, field: str) -> str:
    text = str(value or "")
    if len(text) > maximum:
        raise ValueError(f"{field} exceeds VARCHAR({maximum})")
    return text


def stable_id(run_id: str, ordinal: int) -> str:
    return str(uuid.uuid5(uuid.UUID(run_id), f"tool-call:{ordinal}"))


def datetime2(value: Any) -> str:
    literal = sql_server_text(str(value))
    return f"CONVERT(datetime2(7), CONVERT(datetimeoffset(7), {literal}, 127))"


def optional_datetime2(value: Any) -> str:
    return "NULL" if value is None or str(value).strip() == "" else datetime2(value)


def normalized_datetime(value: Any) -> datetime:
    if isinstance(value, datetime):
        parsed = value
    else:
        parsed = datetime.fromisoformat(str(value).strip().replace("Z", "+00:00"))
    if parsed.tzinfo is not None:
        parsed = parsed.astimezone(timezone.utc).replace(tzinfo=None)
    return parsed


def strings_in(value: Any) -> set[str]:
    if isinstance(value, str):
        return {value}
    if isinstance(value, dict):
        return set().union(*(strings_in(item) for item in value.values())) if value else set()
    if isinstance(value, list):
        return set().union(*(strings_in(item) for item in value)) if value else set()
    return set()


def write_varchar_guards(script: Any, values: set[str]) -> None:
    for value in sorted(values):
        literal = sql_server_text(value)
        script.write(
            "    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), "
            f"CONVERT(VARCHAR(MAX), {literal}))) <> CONVERT(VARBINARY(MAX), {literal})\n"
            "        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR "
            "under the current database collation.', 1;\n"
        )


def load_source_rows(connection_string: str) -> list[tuple[str, str, Any, str, str]]:
    try:
        import pyodbc
    except ImportError as error:
        raise RuntimeError("pyodbc is required: py -3 -m pip install pyodbc") from error

    with pyodbc.connect(connection_string, autocommit=True) as connection:
        cursor = connection.cursor()
        cursor.execute(
            "SELECT ID, AgentId, StartedAtUtc, Status, DocumentJson "
            "FROM dbo.AgAgentRunAudit WHERE DocumentJson IS NOT NULL "
            "ORDER BY AgentId, StartedAtUtc, ID"
        )
        return [
            (str(row[0]), str(row[1]), row[2], str(row[3]), str(row[4]))
            for row in cursor.fetchall()
        ]


def source_hash(rows: list[tuple[str, str, Any, str, str]]) -> str:
    digest = hashlib.sha256()
    for run_id, agent_id, started_at, status, document_json in rows:
        serializable = (run_id, agent_id, str(started_at), status, document_json)
        digest.update(json.dumps(serializable, ensure_ascii=False, separators=(",", ":")).encode("utf-8"))
        digest.update(b"\n")
    return digest.hexdigest()


def main() -> None:
    args = parse_args()
    output = args.output.resolve()
    connection_string = os.environ.get(args.connection_env)
    if not connection_string:
        raise RuntimeError(
            f"Environment variable {args.connection_env!r} must contain the ODBC connection string"
        )

    rows = load_source_rows(connection_string)
    snapshot_hash = source_hash(rows)
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("w", encoding="utf-8-sig", newline="\n") as script:
        script.write("-- Normalize Agent run audits exported from current SQL Server data.\n")
        script.write(f"-- Source row-set SHA-256: {snapshot_hash}\n")
        script.write("-- Run 048 first, then this script, then Data/049, 050, and 051.\n\n")
        script.write("SET NOCOUNT ON;\nSET XACT_ABORT ON;\n\n")
        script.write("IF COL_LENGTH(N'dbo.AgAgentRunAudit', N'DocumentJson') IS NULL\n")
        script.write("    THROW 51920, N'DocumentJson is absent; Agent run audit cutover was already finalized.', 1;\n\n")
        script.write("BEGIN TRY\n    BEGIN TRANSACTION;\n\n")
        script.write("    IF OBJECT_ID(N'dbo.AgAgentRunAuditNormalizationCheckpoint', N'U') IS NULL\n")
        script.write("        CREATE TABLE dbo.AgAgentRunAuditNormalizationCheckpoint (RunId CHAR(36) NOT NULL PRIMARY KEY);\n\n")

        for run_id_source, agent_id_source, started_at_source, status_source, document_json in rows:
            document = json.loads(document_json)
            run_id = guid(document.get("runId"), "RunId")
            agent_id = guid(document.get("agentId"), "AgentId")
            if run_id.casefold() != guid(run_id_source, "Source.RunId").casefold():
                raise ValueError(f"Agent run audit {run_id_source} has a mismatched document run id")
            if agent_id.casefold() != guid(agent_id_source, "Source.AgentId").casefold():
                raise ValueError(f"Agent run audit {run_id_source} has a mismatched document Agent id")
            if normalized_datetime(document["startedAtUtc"]) != normalized_datetime(started_at_source):
                raise ValueError(f"Agent run audit {run_id_source} has a mismatched start time")
            status = enum_name(document.get("status"), RUN_STATUS_BY_ORDINAL, "Status")
            if status.casefold() != enum_name(status_source, RUN_STATUS_BY_ORDINAL, "Source.Status").casefold():
                raise ValueError(f"Agent run audit {run_id_source} has a mismatched status")

            agent_version_id = guid(document.get("agentVersionId"), "AgentVersionId")
            agent_code = bounded(document.get("agentCode"), 128, "AgentCode")
            input_sha256 = bounded(document.get("inputSha256"), 64, "InputSha256")
            error_code = bounded(document.get("errorCode"), 128, "ErrorCode")
            tool_calls = list(document.get("toolCalls") or [])
            declared_tool_count = int(document.get("toolCallCount") or 0)
            if declared_tool_count != len(tool_calls):
                raise ValueError(f"Agent run audit {run_id_source} has an inconsistent tool-call count")

            script.write(f"    -- Agent run audit {run_id}\n")
            write_varchar_guards(script, strings_in(document))
            script.write(
                "    UPDATE dbo.AgAgentRunAudit SET\n"
                f"        AgentVersionId = {sql_server_text(agent_version_id)},\n"
                f"        AgentCode = {sql_server_text(agent_code)},\n"
                f"        Status = {sql_server_text(status)},\n"
                f"        StartedAtUtc = {datetime2(document['startedAtUtc'])},\n"
                f"        FinishedAtUtc = {optional_datetime2(document.get('finishedAtUtc'))},\n"
                f"        InputSha256 = {sql_server_text(input_sha256)},\n"
                f"        OutputCharacters = {int(document.get('outputCharacters') or 0)},\n"
                f"        ToolCallCount = {declared_tool_count},\n"
                f"        ErrorCode = {sql_server_text(error_code)}\n"
                f"    WHERE ID = {sql_server_text(run_id)} AND AgentId = {sql_server_text(agent_id)};\n"
                "    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;\n"
                f"    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = {sql_server_text(run_id)};\n"
            )
            for ordinal, tool_call in enumerate(tool_calls):
                tool_version_id = guid(tool_call.get("toolVersionId"), "ToolCall.ToolVersionId")
                tool_name = bounded(tool_call.get("toolName"), 256, "ToolCall.ToolName")
                risk = enum_name(tool_call.get("risk"), RISK_BY_ORDINAL, "ToolCall.Risk")
                tool_status = enum_name(tool_call.get("status"), EVENT_KIND_BY_ORDINAL, "ToolCall.Status")
                tool_error = bounded(tool_call.get("errorCode"), 128, "ToolCall.ErrorCode")
                script.write(
                    "    INSERT INTO dbo.AgAgentToolCallAudit "
                    "(ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)\n"
                    f"    VALUES ({sql_server_text(stable_id(run_id, ordinal))}, {sql_server_text(run_id)}, {ordinal}, "
                    f"{sql_server_text(tool_version_id)}, {sql_server_text(tool_name)}, {sql_server_text(risk)}, "
                    f"{sql_server_text(tool_status)}, {datetime2(tool_call['startedAtUtc'])}, "
                    f"{datetime2(tool_call['finishedAtUtc'])}, {sql_server_text(tool_error)});\n"
                )
            script.write(
                "    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint "
                f"WHERE RunId = {sql_server_text(run_id)})\n"
                "        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) "
                f"VALUES ({sql_server_text(run_id)});\n\n"
            )

        script.write("    COMMIT TRANSACTION;\nEND TRY\nBEGIN CATCH\n")
        script.write("    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;\n    THROW;\nEND CATCH;\n")

    print(f"Created: {output}")
    print(f"Source rows: {len(rows)}")
    print(f"Source row-set SHA-256: {snapshot_hash}")


if __name__ == "__main__":
    main()
