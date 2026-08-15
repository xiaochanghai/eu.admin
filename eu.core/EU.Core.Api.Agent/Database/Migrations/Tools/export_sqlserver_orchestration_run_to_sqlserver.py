#!/usr/bin/env python3
"""Generate normalized Orchestration Run SQL from current SQL Server rows."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import uuid
from pathlib import Path
from typing import Any

from export_sqlite_to_sqlserver import sql_server_text


RUN_STATUS_BY_ORDINAL = {0: "Running", 1: "Completed", 2: "Failed", 3: "Cancelled"}
NODE_STATUS_BY_ORDINAL = {
    0: "Pending",
    1: "Running",
    2: "Completed",
    3: "Failed",
    4: "Cancelled",
}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("output", type=Path, help="Destination Orchestration Run normalization SQL")
    parser.add_argument(
        "--connection-env",
        default="ORCHESTRATION_RUN_MIGRATION_SQLSERVER_ODBC",
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


def stable_id(parent_id: str, value: str) -> str:
    return str(uuid.uuid5(uuid.UUID(parent_id), value))


def datetime2(value: Any) -> str:
    literal = sql_server_text(str(value))
    return f"CONVERT(datetime2(7), CONVERT(datetimeoffset(7), {literal}, 127))"


def optional_datetime2(value: Any) -> str:
    return "NULL" if value is None or str(value).strip() == "" else datetime2(value)


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
            "        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR "
            "under the current database collation.', 1;\n"
        )


def load_source_rows(connection_string: str) -> list[tuple[str, str, str, str]]:
    try:
        import pyodbc
    except ImportError as error:
        raise RuntimeError("pyodbc is required: py -3 -m pip install pyodbc") from error

    with pyodbc.connect(connection_string, autocommit=True) as connection:
        cursor = connection.cursor()
        cursor.execute(
            "SELECT ID, OrchestrationId, StartedAtUtc, DocumentJson "
            "FROM dbo.AgOrchestrationRun WHERE DocumentJson IS NOT NULL "
            "ORDER BY OrchestrationId, StartedAtUtc, ID"
        )
        return [(str(row[0]), str(row[1]), str(row[2]), str(row[3])) for row in cursor.fetchall()]


def source_hash(rows: list[tuple[str, str, str, str]]) -> str:
    digest = hashlib.sha256()
    for row in rows:
        digest.update(json.dumps(row, ensure_ascii=False, separators=(",", ":")).encode("utf-8"))
        digest.update(b"\n")
    return digest.hexdigest()


def insert_nodes(script: Any, run_id: str, nodes: list[dict[str, Any]]) -> None:
    for ordinal, node in enumerate(nodes):
        row_id = stable_id(run_id, f"node:{ordinal}")
        script.write(
            "    INSERT INTO dbo.AgOrchestrationRunNode "
            "(ID, RunId, Ordinal, NodeId, NodeName, AgentId, AgentVersionId, Status, Attempts, "
            "StartedAtUtc, FinishedAtUtc, OutputCharacters, InputSha256, ErrorCode)\n"
            f"    VALUES ({sql_server_text(row_id)}, {sql_server_text(run_id)}, {ordinal}, "
            f"{sql_server_text(str(node.get('nodeId') or ''))}, "
            f"{sql_server_text(str(node.get('nodeName') or ''))}, "
            f"{sql_server_text(str(node['agentId']))}, {sql_server_text(str(node['agentVersionId']))}, "
            f"{sql_server_text(enum_name(node.get('status'), NODE_STATUS_BY_ORDINAL, 'Node.Status'))}, "
            f"{int(node.get('attempts') or 0)}, {optional_datetime2(node.get('startedAtUtc'))}, "
            f"{optional_datetime2(node.get('finishedAtUtc'))}, {int(node.get('outputCharacters') or 0)}, "
            f"{sql_server_text(str(node.get('inputSha256') or ''))}, "
            f"{sql_server_text(str(node.get('errorCode') or ''))});\n"
        )


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
        script.write("-- Normalize Orchestration Runs exported from current SQL Server data.\n")
        script.write(f"-- Source row-set SHA-256: {snapshot_hash}\n")
        script.write("-- Run 040 and 041 first, then this script, then Data/042.\n\n")
        script.write("SET NOCOUNT ON;\nSET XACT_ABORT ON;\n\n")
        script.write("IF COL_LENGTH(N'dbo.AgOrchestrationRun', N'DocumentJson') IS NULL\n")
        script.write("    THROW 51820, N'DocumentJson is absent; Orchestration Run cutover was already finalized.', 1;\n\n")
        script.write("BEGIN TRY\n    BEGIN TRANSACTION;\n\n")
        script.write("    IF OBJECT_ID(N'dbo.AgOrchestrationRunNormalizationCheckpoint', N'U') IS NULL\n")
        script.write("        CREATE TABLE dbo.AgOrchestrationRunNormalizationCheckpoint (RunId CHAR(36) NOT NULL PRIMARY KEY);\n\n")

        for run_id, orchestration_id, _, document_json in rows:
            document = json.loads(document_json)
            if str(document.get("id", "")).casefold() != run_id.casefold():
                raise ValueError(f"Orchestration Run {run_id} has a mismatched document id")
            if str(document.get("orchestrationId", "")).casefold() != orchestration_id.casefold():
                raise ValueError(f"Orchestration Run {run_id} has a mismatched orchestration id")

            script.write(f"    -- Orchestration Run {run_id}\n")
            write_varchar_guards(script, strings_in(document))
            script.write(
                "    UPDATE dbo.AgOrchestrationRun SET\n"
                f"        OrchestrationVersionId = {sql_server_text(str(document['orchestrationVersionId']))},\n"
                f"        OrchestrationCode = {sql_server_text(str(document.get('orchestrationCode') or ''))},\n"
                f"        Status = {sql_server_text(enum_name(document.get('status'), RUN_STATUS_BY_ORDINAL, 'Status'))},\n"
                f"        StartedAtUtc = {datetime2(document['startedAtUtc'])},\n"
                f"        FinishedAtUtc = {optional_datetime2(document.get('finishedAtUtc'))},\n"
                f"        InputSha256 = {sql_server_text(str(document.get('inputSha256') or ''))},\n"
                f"        ErrorCode = {sql_server_text(str(document.get('errorCode') or ''))}\n"
                f"    WHERE ID = {sql_server_text(run_id)} AND OrchestrationId = {sql_server_text(orchestration_id)};\n"
                "    IF @@ROWCOUNT <> 1 THROW 51821, N'Orchestration Run source row was not found.', 1;\n"
                f"    DELETE FROM dbo.AgOrchestrationRunNode WHERE RunId = {sql_server_text(run_id)};\n"
            )
            insert_nodes(script, run_id, list(document.get("nodes") or []))
            script.write(
                "    IF NOT EXISTS (SELECT 1 FROM dbo.AgOrchestrationRunNormalizationCheckpoint "
                f"WHERE RunId = {sql_server_text(run_id)})\n"
                "        INSERT INTO dbo.AgOrchestrationRunNormalizationCheckpoint (RunId) "
                f"VALUES ({sql_server_text(run_id)});\n\n"
            )

        script.write("    COMMIT TRANSACTION;\nEND TRY\nBEGIN CATCH\n")
        script.write("    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;\n    THROW;\nEND CATCH;\n")

    print(f"Created: {output}")
    print(f"Source rows: {len(rows)}")
    print(f"Source row-set SHA-256: {snapshot_hash}")


if __name__ == "__main__":
    main()
