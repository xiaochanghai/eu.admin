#!/usr/bin/env python3
"""Generate Knowledge normalization SQL from the current SQL Server aggregate rows."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
from typing import Any

from export_sqlite_to_sqlserver import sql_server_text, sql_server_utc_datetime


STATUS_BY_ORDINAL = {
    0: "Enabled",
    1: "Disabled",
    2: "Archived",
}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("output", type=Path, help="Destination Knowledge normalization .sql file")
    parser.add_argument(
        "--connection-env",
        default="KNOWLEDGE_MIGRATION_SQLSERVER_ODBC",
        help="Environment variable containing an ODBC SQL Server connection string",
    )
    return parser.parse_args()


def guid(value: object) -> str:
    return f"CONVERT(uniqueidentifier, {sql_server_text(str(value))})"


def normalize_status(value: Any) -> str:
    if isinstance(value, bool):
        raise ValueError(f"Knowledge status {value!r} is invalid")
    if isinstance(value, int):
        if value in STATUS_BY_ORDINAL:
            return STATUS_BY_ORDINAL[value]
        raise ValueError(f"Knowledge status ordinal {value} is invalid")

    text = str(value).strip()
    if text.lstrip("-").isdigit():
        ordinal = int(text)
        if ordinal in STATUS_BY_ORDINAL:
            return STATUS_BY_ORDINAL[ordinal]
    for name in STATUS_BY_ORDINAL.values():
        if text.casefold() == name.casefold():
            return name
    raise ValueError(f"Knowledge status {value!r} is invalid")


def load_source_rows(connection_string: str) -> list[tuple[str, str, int, str]]:
    try:
        import pyodbc
    except ImportError as error:
        raise RuntimeError("pyodbc is required: py -3 -m pip install pyodbc") from error

    with pyodbc.connect(connection_string, autocommit=True) as connection:
        cursor = connection.cursor()
        cursor.execute(
            "SELECT ID, Code, LogicalRevision, DocumentJson "
            "FROM dbo.AgKnowledgeBaseDefinition "
            "WHERE DocumentJson IS NOT NULL "
            "ORDER BY Code, ID"
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
        script.write("-- Normalize Knowledge documents exported from the current SQL Server source.\n")
        script.write(f"-- Source row-set SHA-256: {snapshot_hash}\n")
        script.write("-- Run SQL Server 015 and 016 first, then this script, then Data/017.\n\n")
        script.write("SET NOCOUNT ON;\nSET XACT_ABORT ON;\n\n")
        script.write("IF OBJECT_ID(N'dbo.AgKnowledgeBaseDefinition', N'U') IS NULL\n")
        script.write("   OR OBJECT_ID(N'dbo.AgKnowledgeDocument', N'U') IS NULL\n")
        script.write("   OR OBJECT_ID(N'dbo.AgKnowledgeChunk', N'U') IS NULL\n")
        script.write("    THROW 51310, N'Knowledge normalized tables are missing.', 1;\n")
        script.write("IF COL_LENGTH(N'dbo.AgKnowledgeBaseDefinition', N'DocumentJson') IS NULL\n")
        script.write("    THROW 51311, N'DocumentJson is absent; Knowledge cutover was already finalized.', 1;\n\n")
        script.write("BEGIN TRY\n    BEGIN TRANSACTION;\n\n")
        script.write("    IF OBJECT_ID(N'dbo.AgKnowledgeNormalizationCheckpoint', N'U') IS NULL\n")
        script.write("        CREATE TABLE dbo.AgKnowledgeNormalizationCheckpoint (ID INT NOT NULL PRIMARY KEY);\n\n")

        for row_id, code, logical_revision, document_json in rows:
            document = json.loads(document_json)
            if str(document.get("id", "")).casefold() != row_id.casefold():
                raise ValueError(f"Knowledge Base {row_id} has a mismatched document id")
            if str(document.get("code", "")) != code:
                raise ValueError(f"Knowledge Base {row_id} has a mismatched document code")
            if int(document.get("logicalRevision", -1)) != logical_revision:
                raise ValueError(f"Knowledge Base {row_id} has a mismatched document revision")

            base_id = guid(row_id)
            source_document_literal = sql_server_text(document_json)
            script.write(f"    -- Knowledge Base {row_id}\n")
            script.write("    UPDATE dbo.AgKnowledgeBaseDefinition\n    SET ")
            assignments = [
                f"Name = {sql_server_text(str(document.get('name') or ''))}",
                f"Description = {sql_server_text(str(document.get('description') or ''))}",
                f"Status = {sql_server_text(normalize_status(document['status']))}",
                f"IndexedAtUtc = {sql_server_utc_datetime(document.get('indexedAtUtc'))}",
            ]
            script.write(",\n        ".join(assignments))
            script.write(
                f"\n    WHERE ID = {base_id}\n"
                f"      AND Code = {sql_server_text(code)}\n"
                f"      AND LogicalRevision = {logical_revision}\n"
                "      AND CONVERT(varbinary(max), DocumentJson) = "
                f"CONVERT(varbinary(max), {source_document_literal});\n"
            )
            script.write(
                "    IF @@ROWCOUNT <> 1 "
                "THROW 51312, N'Knowledge Base source identity changed; regenerate the data script.', 1;\n"
            )
            script.write(f"    DELETE FROM dbo.AgKnowledgeChunk WHERE KnowledgeBaseId = {base_id};\n")
            script.write(f"    DELETE FROM dbo.AgKnowledgeDocument WHERE KnowledgeBaseId = {base_id};\n\n")

            documents = list(document.get("documents") or [])
            for ordinal, item in enumerate(documents):
                script.write(
                    "    INSERT INTO dbo.AgKnowledgeDocument "
                    "(ID, KnowledgeBaseId, Ordinal, FileName, MediaType, Sha256, Content, ImportedAtUtc)\n"
                )
                script.write(
                    f"    VALUES ({guid(item['id'])}, {base_id}, {ordinal}, "
                    f"{sql_server_text(str(item.get('fileName') or ''))}, "
                    f"{sql_server_text(str(item.get('mediaType') or ''))}, "
                    f"{sql_server_text(str(item.get('sha256') or ''))}, "
                    f"{sql_server_text(str(item.get('content') or ''))}, "
                    f"{sql_server_utc_datetime(item.get('importedAtUtc'))});\n"
                )

            for item in document.get("chunks") or []:
                script.write(
                    "    INSERT INTO dbo.AgKnowledgeChunk "
                    "(ID, KnowledgeBaseId, DocumentId, Sequence, Content)\n"
                )
                script.write(
                    f"    VALUES ({guid(item['id'])}, {base_id}, {guid(item['documentId'])}, "
                    f"{int(item['sequence'])}, {sql_server_text(str(item.get('content') or ''))});\n"
                )
            script.write("\n")

        script.write("    IF NOT EXISTS (SELECT 1 FROM dbo.AgKnowledgeNormalizationCheckpoint WHERE ID = 1)\n")
        script.write("        INSERT INTO dbo.AgKnowledgeNormalizationCheckpoint (ID) VALUES (1);\n\n")
        script.write("    COMMIT TRANSACTION;\nEND TRY\nBEGIN CATCH\n")
        script.write("    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;\n    THROW;\nEND CATCH;\n")

    print(f"Created: {output}")
    print(f"Source rows: {len(rows)}")
    print(f"Source row-set SHA-256: {snapshot_hash}")


if __name__ == "__main__":
    main()
