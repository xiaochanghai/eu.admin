#!/usr/bin/env python3
"""Generate normalized Orchestration SQL from current SQL Server aggregate rows."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import uuid
from pathlib import Path
from typing import Any

from export_sqlite_to_sqlserver import sql_server_text


STATUS_BY_ORDINAL = {0: "Enabled", 1: "Disabled", 2: "Archived"}
INPUT_MODE_BY_ORDINAL = {0: "InitialInput", 1: "PreviousOutput", 2: "Template"}
CONDITION_BY_ORDINAL = {
    0: "Always",
    1: "Succeeded",
    2: "Failed",
    3: "OutputContains",
}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("output", type=Path, help="Destination Orchestration normalization .sql file")
    parser.add_argument(
        "--connection-env",
        default="ORCHESTRATION_MIGRATION_SQLSERVER_ODBC",
        help="Environment variable containing an ODBC SQL Server connection string",
    )
    return parser.parse_args()


def guid(value: object) -> str:
    return f"CONVERT(uniqueidentifier, {sql_server_text(str(value))})"


def enum_name(value: Any, names: dict[int, str], field: str) -> str:
    if isinstance(value, bool):
        raise ValueError(f"{field} {value!r} is invalid")
    if isinstance(value, int):
        if value in names:
            return names[value]
        raise ValueError(f"{field} ordinal {value} is invalid")
    text = str(value).strip()
    if text.lstrip("-").isdigit() and int(text) in names:
        return names[int(text)]
    for name in names.values():
        if text.casefold() == name.casefold():
            return name
    raise ValueError(f"{field} {value!r} is invalid")


def stable_child_id(version_id: str, kind: str, ordinal: int) -> str:
    return str(uuid.uuid5(uuid.UUID(version_id), f"{kind}:{ordinal}"))


def load_source_rows(connection_string: str) -> list[tuple[str, str, int, str]]:
    try:
        import pyodbc
    except ImportError as error:
        raise RuntimeError("pyodbc is required: py -3 -m pip install pyodbc") from error

    with pyodbc.connect(connection_string, autocommit=True) as connection:
        cursor = connection.cursor()
        cursor.execute(
            "SELECT ID, Code, LogicalRevision, DocumentJson "
            "FROM dbo.AgOrchestrationDefinition "
            "WHERE DocumentJson IS NOT NULL ORDER BY Code, ID"
        )
        return [
            (str(row[0]), str(row[1]), int(row[2]), str(row[3]))
            for row in cursor.fetchall()
        ]


def source_hash(rows: list[tuple[str, str, int, str]]) -> str:
    digest = hashlib.sha256()
    for row in rows:
        digest.update(json.dumps(row, ensure_ascii=False, separators=(",", ":")).encode("utf-8"))
        digest.update(b"\n")
    return digest.hexdigest()


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
            "        THROW 51413, N'Orchestration text cannot be represented by VARCHAR "
            "under the current database collation.', 1;\n"
        )


def insert_version(
    script: Any,
    orchestration_id: str,
    orchestration_code: str,
    version: dict[str, Any],
    ordinal: int,
) -> None:
    version_id = str(version["id"])
    is_draft = bool(version["isDraft"])
    script.write(
        "    INSERT INTO dbo.AgOrchestrationVersion "
        "(ID, OrchestrationId, Ordinal, Label, IsDraft, StartNodeId)\n"
        f"    VALUES ({guid(version_id)}, {guid(orchestration_id)}, {ordinal}, "
        f"{sql_server_text(str(version.get('label') or ''))}, {1 if is_draft else 0}, "
        f"{sql_server_text(str(version.get('startNodeId') or ''))});\n"
    )

    nodes = list(version.get("nodes") or [])
    for node_ordinal, node in enumerate(nodes):
        script.write(
            "    INSERT INTO dbo.AgOrchestrationNode "
            "(ID, OrchestrationId, VersionId, Ordinal, NodeId, Name, AgentId, InputMode, "
            "InputTemplate, MaximumRetries, TimeoutSeconds)\n"
            f"    VALUES ({guid(stable_child_id(version_id, 'node', node_ordinal))}, "
            f"{guid(orchestration_id)}, {guid(version_id)}, {node_ordinal}, "
            f"{sql_server_text(str(node.get('id') or ''))}, "
            f"{sql_server_text(str(node.get('name') or ''))}, {guid(node['agentId'])}, "
            f"{sql_server_text(enum_name(node['inputMode'], INPUT_MODE_BY_ORDINAL, 'InputMode'))}, "
            f"{sql_server_text(str(node.get('inputTemplate') or ''))}, "
            f"{int(node['maximumRetries'])}, {int(node['timeoutSeconds'])});\n"
        )

    edges = list(version.get("edges") or [])
    for edge_ordinal, edge in enumerate(edges):
        script.write(
            "    INSERT INTO dbo.AgOrchestrationEdge "
            "(ID, OrchestrationId, VersionId, Ordinal, FromNodeId, ToNodeId, Condition, "
            "ConditionValue, SortOrder)\n"
            f"    VALUES ({guid(stable_child_id(version_id, 'edge', edge_ordinal))}, "
            f"{guid(orchestration_id)}, {guid(version_id)}, {edge_ordinal}, "
            f"{sql_server_text(str(edge.get('fromNodeId') or ''))}, "
            f"{sql_server_text(str(edge.get('toNodeId') or ''))}, "
            f"{sql_server_text(enum_name(edge['condition'], CONDITION_BY_ORDINAL, 'Condition'))}, "
            f"{sql_server_text(str(edge.get('conditionValue') or ''))}, {int(edge['order'])});\n"
        )

    snapshot = version.get("snapshot")
    if is_draft:
        if snapshot is not None:
            raise ValueError(f"Draft Orchestration version {version_id} must not have a snapshot")
        return
    if not isinstance(snapshot, dict):
        raise ValueError(f"Published Orchestration version {version_id} is missing its snapshot")
    if str(snapshot.get("versionId", "")).casefold() != version_id.casefold():
        raise ValueError(f"Published Orchestration version {version_id} has a mismatched snapshot id")
    if str(snapshot.get("orchestrationCode", "")) != orchestration_code:
        raise ValueError(f"Published Orchestration version {version_id} has a mismatched code")
    if snapshot.get("startNodeId") != version.get("startNodeId"):
        raise ValueError(f"Published Orchestration version {version_id} has a mismatched start node")
    if list(snapshot.get("nodes") or []) != nodes or list(snapshot.get("edges") or []) != edges:
        raise ValueError(f"Published Orchestration version {version_id} has a mismatched graph snapshot")

    for binding_ordinal, binding in enumerate(snapshot.get("agents") or []):
        script.write(
            "    INSERT INTO dbo.AgOrchestrationAgentBinding "
            "(ID, OrchestrationId, VersionId, Ordinal, AgentId, AgentVersionId)\n"
            f"    VALUES ({guid(stable_child_id(version_id, 'binding', binding_ordinal))}, "
            f"{guid(orchestration_id)}, {guid(version_id)}, {binding_ordinal}, "
            f"{guid(binding['agentId'])}, {guid(binding['agentVersionId'])});\n"
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
        script.write("-- Normalize Orchestration definitions exported from current SQL Server data.\n")
        script.write(f"-- Source row-set SHA-256: {snapshot_hash}\n")
        script.write("-- Run SQL Server 020 and 021 first, then this script, then Data/022.\n\n")
        script.write("SET NOCOUNT ON;\nSET XACT_ABORT ON;\n\n")
        script.write("IF COL_LENGTH(N'dbo.AgOrchestrationDefinition', N'DocumentJson') IS NULL\n")
        script.write("    THROW 51411, N'DocumentJson is absent; Orchestration cutover was already finalized.', 1;\n\n")
        script.write("BEGIN TRY\n    BEGIN TRANSACTION;\n\n")
        script.write("    IF OBJECT_ID(N'dbo.AgOrchestrationNormalizationCheckpoint', N'U') IS NULL\n")
        script.write("        CREATE TABLE dbo.AgOrchestrationNormalizationCheckpoint (OrchestrationId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY);\n\n")

        for row_id, code, logical_revision, document_json in rows:
            document = json.loads(document_json)
            if str(document.get("id", "")).casefold() != row_id.casefold():
                raise ValueError(f"Orchestration {row_id} has a mismatched document id")
            if str(document.get("code", "")) != code:
                raise ValueError(f"Orchestration {row_id} has a mismatched document code")

            script.write(f"    -- Orchestration {row_id}\n")
            write_varchar_guards(script, strings_in(document))
            script.write(
                "    UPDATE dbo.AgOrchestrationDefinition SET\n"
                f"        Name = {sql_server_text(str(document.get('name') or ''))},\n"
                f"        Description = {sql_server_text(str(document.get('description') or ''))},\n"
                f"        Status = {sql_server_text(enum_name(document['status'], STATUS_BY_ORDINAL, 'Status'))},\n"
                f"        LogicalRevision = {int(document.get('logicalRevision', logical_revision))}\n"
                f"    WHERE ID = {guid(row_id)} AND Code = {sql_server_text(code)};\n"
                "    IF @@ROWCOUNT <> 1 THROW 51412, N'Orchestration source row was not found.', 1;\n"
            )
            script.write(
                "    DELETE binding FROM dbo.AgOrchestrationAgentBinding binding "
                f"WHERE binding.OrchestrationId = {guid(row_id)};\n"
                "    DELETE edge FROM dbo.AgOrchestrationEdge edge "
                f"WHERE edge.OrchestrationId = {guid(row_id)};\n"
                "    DELETE node FROM dbo.AgOrchestrationNode node "
                f"WHERE node.OrchestrationId = {guid(row_id)};\n"
                "    DELETE version FROM dbo.AgOrchestrationVersion version "
                f"WHERE version.OrchestrationId = {guid(row_id)};\n"
            )

            insert_version(script, row_id, code, dict(document["draft"]), 0)
            for ordinal, version in enumerate(document.get("publishedVersions") or [], start=1):
                insert_version(script, row_id, code, dict(version), ordinal)
            script.write(
                "    IF NOT EXISTS (SELECT 1 FROM dbo.AgOrchestrationNormalizationCheckpoint "
                f"WHERE OrchestrationId = {guid(row_id)})\n"
                "        INSERT INTO dbo.AgOrchestrationNormalizationCheckpoint (OrchestrationId) "
                f"VALUES ({guid(row_id)});\n\n"
            )

        script.write("    COMMIT TRANSACTION;\nEND TRY\nBEGIN CATCH\n")
        script.write("    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;\n    THROW;\nEND CATCH;\n")

    print(f"Created: {output}")
    print(f"Source rows: {len(rows)}")
    print(f"Source row-set SHA-256: {snapshot_hash}")


if __name__ == "__main__":
    main()
