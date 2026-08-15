#!/usr/bin/env python3
"""Export the EU.Core Agent SQLite database as a SQL Server data script."""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import re
import sqlite3
import tempfile
import uuid
from contextlib import closing
from datetime import datetime, timezone
from pathlib import Path
from typing import BinaryIO, Iterable, TextIO


TABLES: tuple[tuple[str, str], ...] = (
    ("agent_definitions", "AgAgentDefinition"),
    ("skill_definitions", "AgSkillDefinition"),
    ("mcp_server_definitions", "AgMcpServerDefinition"),
    ("knowledge_base_definitions", "AgKnowledgeBaseDefinition"),
    ("agent_run_audits", "AgAgentRunAudit"),
    ("agent_operation_audits", "AgAgentOperationAudit"),
    ("orchestration_definitions", "AgOrchestrationDefinition"),
    ("orchestration_runs", "AgOrchestrationRun"),
    ("orchestration_run_details", "AgOrchestrationRunDetail"),
    ("orchestration_node_attempts", "AgOrchestrationNodeAttempt"),
    ("orchestration_tool_calls", "AgOrchestrationToolCall"),
    ("chat_conversations", "AgChatConversation"),
    ("chat_messages", "AgChatMessage"),
    ("unified_entry_runs", "AgUnifiedEntryRun"),
    ("unified_agent_runs", "AgUnifiedAgentRun"),
    ("unified_orchestration_links", "AgUnifiedOrchestrationLink"),
    ("unified_tool_calls", "AgUnifiedToolCall"),
    ("unified_run_events", "AgUnifiedRunEvent"),
    ("main_agent_assignment", "AgMainAgentAssignment"),
    ("tool_approval_requests", "AgToolApprovalRequest"),
    ("tool_approval_payloads", "AgToolApprovalPayload"),
    ("tool_approval_decisions", "AgToolApprovalDecision"),
    ("tool_approval_execution_results", "AgToolApprovalExecutionResult"),
    ("evaluation_suites", "AgEvaluationSuite"),
    ("evaluation_batches", "AgEvaluationBatch"),
    ("evaluation_model_judgements", "AgEvaluationModelJudgement"),
    ("api_idempotency", "AgApiIdempotency"),
)

SPECIAL_WORDS = {
    "api": "Api",
    "id": "Id",
    "json": "Json",
    "mcp": "Mcp",
    "sha256": "Sha256",
    "utf8": "Utf8",
    "utc": "Utc",
}

IDENTIFIER_PATTERN = re.compile(r"^[A-Za-z_][A-Za-z0-9_]*$")
TEXT_CHUNK_SIZE = 2000
INSERT_BATCH_SIZE = 100


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("source", type=Path, help="Path to eu-core-agent.db")
    parser.add_argument("output", type=Path, help="Destination .sql file")
    return parser.parse_args()


def snake_to_pascal(value: str) -> str:
    words: list[str] = []
    for part in value.split("_"):
        words.append(SPECIAL_WORDS.get(part, part[:1].upper() + part[1:]))
    return "".join(words)


def sqlite_identifier(value: str) -> str:
    if not IDENTIFIER_PATTERN.fullmatch(value):
        raise ValueError(f"Unsafe SQLite identifier: {value}")
    return '"' + value + '"'


def sql_server_identifier(value: str) -> str:
    if not IDENTIFIER_PATTERN.fullmatch(value):
        raise ValueError(f"Unsafe SQL Server identifier: {value}")
    return "[" + value + "]"


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        while block := stream.read(1024 * 1024):
            digest.update(block)
    return digest.hexdigest()


def create_snapshot(source: Path, directory: Path) -> Path:
    temporary = tempfile.NamedTemporaryFile(
        prefix="eu-core-agent-export-",
        suffix=".db",
        delete=False,
        dir=directory,
    )
    temporary.close()
    snapshot = Path(temporary.name)
    source_uri = source.resolve().as_uri() + "?mode=ro"
    with closing(sqlite3.connect(source_uri, uri=True)) as source_connection:
        with closing(sqlite3.connect(snapshot)) as snapshot_connection:
            source_connection.backup(snapshot_connection)
    return snapshot


def flush_text_part(parts: list[str], buffer: list[str]) -> None:
    if not buffer:
        return
    escaped = "".join(buffer).replace("'", "''")
    parts.append("N'" + escaped + "'")
    buffer.clear()


def sql_server_text(value: str) -> str:
    parts: list[str] = []
    buffer: list[str] = []
    for character in value:
        code_point = ord(character)
        if code_point < 32 or code_point == 127:
            flush_text_part(parts, buffer)
            parts.append(f"NCHAR({code_point})")
            continue
        buffer.append(character)
        if len(buffer) >= TEXT_CHUNK_SIZE:
            flush_text_part(parts, buffer)
    flush_text_part(parts, buffer)
    if not parts:
        return "N''"
    if len(parts) == 1:
        return parts[0]
    return "CAST(N'' AS nvarchar(max)) + " + " + ".join(parts)


def sql_server_literal(value: object) -> str:
    if value is None:
        return "NULL"
    if isinstance(value, bytes):
        return "0x" + value.hex().upper()
    if isinstance(value, bool):
        return "1" if value else "0"
    if isinstance(value, int):
        return str(value)
    if isinstance(value, float):
        if not math.isfinite(value):
            raise ValueError(f"SQL Server cannot import non-finite float: {value}")
        return format(value, ".17g")
    if isinstance(value, str):
        return sql_server_text(value)
    raise TypeError(f"Unsupported SQLite value type: {type(value).__name__}")


def get_columns(connection: sqlite3.Connection, table: str) -> list[str]:
    rows = connection.execute(
        f"PRAGMA table_info({sqlite_identifier(table)})"
    ).fetchall()
    if not rows:
        raise RuntimeError(f"SQLite table has no columns: {table}")
    return [str(row[1]) for row in rows]


def write_header(
    output: TextIO,
    source: Path,
    snapshot_hash: str,
    row_counts: dict[str, int],
) -> None:
    generated = datetime.now(timezone.utc).isoformat()
    output.write("-- EU.Core Agent SQLite to SQL Server data import\n")
    output.write(f"-- Source: {source.resolve()}\n")
    output.write(f"-- Snapshot SHA-256: {snapshot_hash}\n")
    output.write(f"-- Generated UTC: {generated}\n")
    output.write(f"-- Total rows: {sum(row_counts.values())}\n")
    output.write("-- Execute 001_initial_schema.sql first and run this script against the same database.\n")
    output.write("-- Target Ag* tables must be empty. The script is transactional.\n\n")
    output.write("SET NOCOUNT ON;\n")
    output.write("SET XACT_ABORT ON;\n\n")
    output.write("BEGIN TRY\n")
    output.write("    BEGIN TRANSACTION;\n\n")


def write_preflight(output: TextIO) -> None:
    output.write("    -- Refuse partial or duplicate imports.\n")
    for _, target in TABLES:
        quoted = sql_server_identifier(target)
        output.write(
            f"    IF OBJECT_ID(N'dbo.{target}', N'U') IS NULL "
            f"THROW 51000, N'Missing target table dbo.{target}.', 1;\n"
        )
        output.write(
            f"    IF EXISTS (SELECT TOP (1) 1 FROM dbo.{quoted}) "
            f"THROW 51001, N'Target table dbo.{target} must be empty.', 1;\n"
        )
    output.write("\n")


def write_table_rows(
    connection: sqlite3.Connection,
    output: TextIO,
    source_table: str,
    target_table: str,
    columns: list[str],
) -> int:
    source_columns = ", ".join(sqlite_identifier(column) for column in columns)
    target_columns = ", ".join(
        sql_server_identifier(snake_to_pascal(column)) for column in columns
    )
    query = (
        f"SELECT {source_columns} FROM {sqlite_identifier(source_table)}"
    )
    cursor = connection.execute(query)
    total = 0
    while True:
        rows = cursor.fetchmany(INSERT_BATCH_SIZE)
        if not rows:
            break
        output.write(
            f"    INSERT INTO dbo.{sql_server_identifier(target_table)} "
            f"({target_columns})\n"
        )
        output.write("    VALUES\n")
        for index, row in enumerate(rows):
            values = ", ".join(sql_server_literal(value) for value in row)
            terminator = ",\n" if index + 1 < len(rows) else ";\n"
            output.write(f"        ({values}){terminator}")
        output.write("\n")
        total += len(rows)
    return total


def write_validation_and_footer(
    output: TextIO, row_counts: dict[str, int]
) -> None:
    output.write("    -- Validate every imported table before commit.\n")
    for source, target in TABLES:
        expected = row_counts[source]
        quoted = sql_server_identifier(target)
        output.write(
            f"    IF (SELECT COUNT_BIG(*) FROM dbo.{quoted}) <> {expected} "
            f"THROW 51002, N'Row-count validation failed for dbo.{target}.', 1;\n"
        )
    output.write("\n    COMMIT TRANSACTION;\n")
    output.write("END TRY\n")
    output.write("BEGIN CATCH\n")
    output.write("    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;\n")
    output.write("    THROW;\n")
    output.write("END CATCH;\n")


def sql_server_utc_datetime(value: str | None) -> str:
    if value is None:
        return "NULL"
    normalized = value.replace("+00:00", "").removesuffix("Z")
    return f"CONVERT(datetime2(7), {sql_server_text(normalized)}, 126)"


def write_mcp_normalization_script(
    connection: sqlite3.Connection,
    output_path: Path,
    source: Path,
    snapshot_hash: str,
) -> None:
    rows = connection.execute(
        'SELECT "id", "document_json" FROM "mcp_server_definitions" ORDER BY "code", "id"'
    ).fetchall()
    output_path = output_path.resolve()
    output_path.parent.mkdir(parents=True, exist_ok=True)
    with output_path.open("w", encoding="utf-8-sig", newline="\n") as output:
        output.write("-- Normalize MCP documents exported from the SQLite source.\n")
        output.write(f"-- Source: {source.resolve()}\n")
        output.write(f"-- Snapshot SHA-256: {snapshot_hash}\n")
        output.write("-- Run SQL Server 010 and 011 first, then this script, then Data/012.\n\n")
        output.write("SET NOCOUNT ON;\nSET XACT_ABORT ON;\n\n")
        output.write("IF OBJECT_ID(N'dbo.AgMcpServerDefinition', N'U') IS NULL\n")
        output.write("   OR OBJECT_ID(N'dbo.AgMcpServerArgument', N'U') IS NULL\n")
        output.write("   OR OBJECT_ID(N'dbo.AgMcpToolVersion', N'U') IS NULL\n")
        output.write("    THROW 51210, N'MCP normalized tables are missing.', 1;\n")
        output.write("IF COL_LENGTH(N'dbo.AgMcpServerDefinition', N'DocumentJson') IS NULL\n")
        output.write("    THROW 51211, N'DocumentJson is absent; MCP cutover was already finalized.', 1;\n\n")
        output.write("BEGIN TRY\n    BEGIN TRANSACTION;\n\n")
        output.write("    IF OBJECT_ID(N'dbo.AgMcpNormalizationCheckpoint', N'U') IS NULL\n")
        output.write("        CREATE TABLE dbo.AgMcpNormalizationCheckpoint (ID INT NOT NULL PRIMARY KEY);\n\n")

        for row_id, document_json in rows:
            document = json.loads(str(document_json))
            server_id = str(row_id)
            server_literal = sql_server_text(server_id)
            last_synced = document.get("lastSyncedAtUtc")
            output.write(f"    -- MCP Server {server_id}\n")
            output.write("    UPDATE dbo.AgMcpServerDefinition\n    SET ")
            assignments = [
                f"Name = {sql_server_text(str(document.get('name') or ''))}",
                f"Description = {sql_server_text(str(document.get('description') or ''))}",
                f"Transport = {sql_server_text(str(document['transport']))}",
                f"Endpoint = {sql_server_text(str(document.get('endpoint') or ''))}",
                f"Command = {sql_server_text(str(document.get('command') or ''))}",
                f"CredentialAlias = {sql_server_text(str(document.get('credentialAlias') or ''))}",
                f"Enabled = {1 if document.get('enabled') else 0}",
                f"Status = {sql_server_text(str(document['status']))}",
                f"LastError = {sql_server_text(str(document.get('lastError') or ''))}",
                f"LastSyncedAtUtc = {sql_server_utc_datetime(last_synced)}",
            ]
            output.write(",\n        ".join(assignments))
            output.write(f"\n    WHERE ID = CONVERT(uniqueidentifier, {server_literal});\n")
            output.write("    IF @@ROWCOUNT <> 1 THROW 51212, N'MCP Server target row is missing.', 1;\n\n")

            arguments = list(document.get("arguments") or [])
            for ordinal, argument in enumerate(arguments):
                argument_id = uuid.uuid5(uuid.UUID(server_id), f"argument:{ordinal}")
                output.write("    UPDATE dbo.AgMcpServerArgument\n")
                output.write(f"    SET [Value] = {sql_server_text(str(argument))}\n")
                output.write(
                    f"    WHERE ServerId = CONVERT(uniqueidentifier, {server_literal}) "
                    f"AND Ordinal = {ordinal};\n"
                )
                output.write("    IF @@ROWCOUNT = 0\n")
                output.write("        INSERT INTO dbo.AgMcpServerArgument (ID, ServerId, Ordinal, [Value])\n")
                output.write(
                    f"        VALUES (CONVERT(uniqueidentifier, N'{argument_id}'), "
                    f"CONVERT(uniqueidentifier, {server_literal}), {ordinal}, "
                    f"{sql_server_text(str(argument))});\n"
                )
            output.write(
                "    DELETE FROM dbo.AgMcpServerArgument "
                f"WHERE ServerId = CONVERT(uniqueidentifier, {server_literal}) "
                f"AND Ordinal >= {len(arguments)};\n\n"
            )

            current_ids = {
                str(tool_id): ordinal
                for ordinal, tool_id in enumerate(document.get("currentToolVersionIds") or [])
            }
            output.write(
                "    UPDATE dbo.AgMcpToolVersion SET CurrentOrdinal = NULL "
                f"WHERE ServerId = CONVERT(uniqueidentifier, {server_literal});\n"
            )
            for history_ordinal, tool in enumerate(document.get("toolVersions") or []):
                tool_id = str(tool["id"])
                current_ordinal = current_ids.get(tool_id)
                current_literal = "NULL" if current_ordinal is None else str(current_ordinal)
                tool_values = {
                    "Name": sql_server_text(str(tool.get("name") or "")),
                    "Description": sql_server_text(str(tool.get("description") or "")),
                    "InputSchemaJson": sql_server_text(str(tool.get("inputSchemaJson") or "{}")),
                    "Risk": sql_server_text(str(tool["risk"])),
                    "Sha256": sql_server_text(str(tool["sha256"])),
                    "DiscoveredAtUtc": sql_server_utc_datetime(tool.get("discoveredAtUtc")),
                }
                output.write("    UPDATE dbo.AgMcpToolVersion\n")
                output.write(
                    f"    SET ServerId = CONVERT(uniqueidentifier, {server_literal}), "
                    f"HistoryOrdinal = {history_ordinal}, CurrentOrdinal = {current_literal},\n"
                    f"        Name = {tool_values['Name']}, Description = {tool_values['Description']},\n"
                    f"        InputSchemaJson = {tool_values['InputSchemaJson']}, Risk = {tool_values['Risk']},\n"
                    f"        Sha256 = {tool_values['Sha256']}, DiscoveredAtUtc = {tool_values['DiscoveredAtUtc']}\n"
                    f"    WHERE ID = CONVERT(uniqueidentifier, {sql_server_text(tool_id)});\n"
                )
                output.write("    IF @@ROWCOUNT = 0\n")
                output.write(
                    "        INSERT INTO dbo.AgMcpToolVersion "
                    "(ID, ServerId, HistoryOrdinal, CurrentOrdinal, Name, Description, "
                    "InputSchemaJson, Risk, Sha256, DiscoveredAtUtc)\n"
                )
                output.write(
                    f"        VALUES (CONVERT(uniqueidentifier, {sql_server_text(tool_id)}), "
                    f"CONVERT(uniqueidentifier, {server_literal}), {history_ordinal}, {current_literal}, "
                    f"{tool_values['Name']}, {tool_values['Description']}, {tool_values['InputSchemaJson']}, "
                    f"{tool_values['Risk']}, {tool_values['Sha256']}, {tool_values['DiscoveredAtUtc']});\n"
                )
            output.write("\n")

        output.write("    IF NOT EXISTS (SELECT 1 FROM dbo.AgMcpNormalizationCheckpoint WHERE ID = 1)\n")
        output.write("        INSERT INTO dbo.AgMcpNormalizationCheckpoint (ID) VALUES (1);\n\n")
        output.write("    COMMIT TRANSACTION;\nEND TRY\nBEGIN CATCH\n")
        output.write("    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;\n    THROW;\nEND CATCH;\n")


def main() -> None:
    args = parse_args()
    source = args.source.resolve()
    output_path = args.output.resolve()
    if not source.is_file():
        raise FileNotFoundError(source)
    output_path.parent.mkdir(parents=True, exist_ok=True)

    snapshot = create_snapshot(source, output_path.parent)
    try:
        snapshot_hash = sha256_file(snapshot)
        with closing(sqlite3.connect(snapshot)) as connection:
            violations = connection.execute("PRAGMA foreign_key_check").fetchall()
            if violations:
                raise RuntimeError(
                    f"SQLite foreign-key check failed with {len(violations)} violation(s)"
                )
            actual_tables = {
                str(row[0])
                for row in connection.execute(
                    "SELECT name FROM sqlite_master "
                    "WHERE type = 'table' AND name NOT LIKE 'sqlite_%'"
                )
            }
            expected_tables = {source_name for source_name, _ in TABLES}
            if actual_tables != expected_tables:
                missing = sorted(expected_tables - actual_tables)
                extra = sorted(actual_tables - expected_tables)
                raise RuntimeError(
                    f"SQLite table mapping is stale; missing={missing}, extra={extra}"
                )

            columns = {
                source_name: get_columns(connection, source_name)
                for source_name, _ in TABLES
            }
            row_counts = {
                source_name: int(
                    connection.execute(
                        f"SELECT COUNT(*) FROM {sqlite_identifier(source_name)}"
                    ).fetchone()[0]
                )
                for source_name, _ in TABLES
            }

            with output_path.open("w", encoding="utf-8-sig", newline="\n") as output:
                write_header(output, source, snapshot_hash, row_counts)
                write_preflight(output)
                exported_counts: dict[str, int] = {}
                for source_name, target_name in TABLES:
                    output.write(
                        f"    -- {source_name} -> dbo.{target_name} "
                        f"({row_counts[source_name]} row(s))\n"
                    )
                    exported_counts[source_name] = write_table_rows(
                        connection,
                        output,
                        source_name,
                        target_name,
                        columns[source_name],
                    )
                if exported_counts != row_counts:
                    raise RuntimeError(
                        f"Exported row counts do not match snapshot: "
                        f"expected={row_counts}, actual={exported_counts}"
                    )
                write_validation_and_footer(output, row_counts)


        print(f"Created: {output_path}")
        print(f"Snapshot SHA-256: {snapshot_hash}")
        print(f"Rows: {sum(row_counts.values())}")
    finally:
        snapshot.unlink(missing_ok=True)


if __name__ == "__main__":
    main()
