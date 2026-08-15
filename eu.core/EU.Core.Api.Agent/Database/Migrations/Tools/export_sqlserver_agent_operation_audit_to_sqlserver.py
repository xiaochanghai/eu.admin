#!/usr/bin/env python3
"""Generate normalized Agent operation audit SQL from SQL Server rows."""

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


LIMITS = {
    "tenantId": 128,
    "userId": 256,
    "correlationId": 128,
    "policy": 512,
    "method": 16,
    "path": 2048,
    "outcome": 32,
    "errorCode": 128,
}

BATCH_SIZE = 100


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("output", type=Path)
    parser.add_argument(
        "--connection-env",
        default="AGENT_OPERATION_AUDIT_MIGRATION_SQLSERVER_ODBC",
    )
    return parser.parse_args()


def guid(value: Any, field: str) -> str:
    try:
        return str(uuid.UUID(str(value)))
    except (ValueError, AttributeError) as error:
        raise ValueError(f"{field} {value!r} is not a GUID") from error


def bounded(document: dict[str, Any], field: str, nullable: bool = False) -> str | None:
    value = document.get(field)
    if value is None and nullable:
        return None
    text = str(value or "")
    if len(text) > LIMITS[field]:
        raise ValueError(f"{field} exceeds VARCHAR({LIMITS[field]})")
    return text


def normalized_datetime(value: Any) -> datetime:
    if isinstance(value, datetime):
        parsed = value
    else:
        parsed = datetime.fromisoformat(str(value).strip().replace("Z", "+00:00"))
    if parsed.tzinfo is not None:
        parsed = parsed.astimezone(timezone.utc).replace(tzinfo=None)
    return parsed


def datetime2(value: Any) -> str:
    literal = sql_server_text(str(value))
    return f"CONVERT(datetime2(7), CONVERT(datetimeoffset(7), {literal}, 127))"


def load_rows(connection_string: str) -> list[tuple[str, str, Any, str, str]]:
    try:
        import pyodbc
    except ImportError as error:
        raise RuntimeError("pyodbc is required: py -3 -m pip install pyodbc") from error
    with pyodbc.connect(connection_string, autocommit=True) as connection:
        cursor = connection.cursor()
        cursor.execute(
            "SELECT ID, TenantId, OccurredAtUtc, Outcome, DocumentJson "
            "FROM dbo.AgAgentOperationAudit WHERE DocumentJson IS NOT NULL "
            "ORDER BY TenantId, OccurredAtUtc, ID"
        )
        return [(str(row[0]), str(row[1]), row[2], str(row[3]), str(row[4])) for row in cursor.fetchall()]


def main() -> None:
    args = parse_args()
    connection_string = os.environ.get(args.connection_env)
    if not connection_string:
        raise RuntimeError(f"Environment variable {args.connection_env!r} is required")
    rows = load_rows(connection_string)
    digest = hashlib.sha256()
    for row in rows:
        digest.update(json.dumps(tuple(map(str, row)), ensure_ascii=False, separators=(",", ":")).encode())
        digest.update(b"\n")

    output = args.output.resolve()
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("w", encoding="utf-8-sig", newline="\n") as script:
        script.write("-- Normalize Agent operation audits exported from SQL Server.\n")
        script.write(f"-- Source row-set SHA-256: {digest.hexdigest()}\n")
        script.write("-- Run 052 first, then this script, then Data/053, 054, and 055.\n\n")
        script.write("SET NOCOUNT ON;\nSET XACT_ABORT ON;\n\n")
        script.write("IF COL_LENGTH(N'dbo.AgAgentOperationAudit', N'DocumentJson') IS NULL\n")
        script.write("    THROW 52010, N'DocumentJson is absent; operation audit cutover was finalized.', 1;\nGO\n\n")
        script.write("    IF OBJECT_ID(N'dbo.AgAgentOperationAuditNormalizationCheckpoint', N'U') IS NULL\n")
        script.write("        CREATE TABLE dbo.AgAgentOperationAuditNormalizationCheckpoint (ID CHAR(36) NOT NULL PRIMARY KEY);\nGO\n\n")
        for index, (source_id, source_tenant, source_time, source_outcome, payload) in enumerate(rows):
            if index % BATCH_SIZE == 0:
                script.write("BEGIN TRY\n    BEGIN TRANSACTION;\n")
            document = json.loads(payload)
            audit_id = guid(document.get("id"), "Id")
            if audit_id.casefold() != guid(source_id, "Source.Id").casefold():
                raise ValueError(f"Audit {source_id} has a mismatched document id")
            tenant = bounded(document, "tenantId")
            outcome = bounded(document, "outcome")
            if tenant != source_tenant or outcome != source_outcome:
                raise ValueError(f"Audit {source_id} source identity/status mismatch")
            if normalized_datetime(document["occurredAtUtc"]) != normalized_datetime(source_time):
                raise ValueError(f"Audit {source_id} has a mismatched occurrence time")
            values = {
                field: bounded(document, field, nullable=(field == "errorCode"))
                for field in LIMITS
            }
            error_sql = "NULL" if values["errorCode"] is None else sql_server_text(values["errorCode"])
            script.write(
                "    UPDATE dbo.AgAgentOperationAudit SET\n"
                f"        TenantId = {sql_server_text(values['tenantId'])}, UserId = {sql_server_text(values['userId'])},\n"
                f"        CorrelationId = {sql_server_text(values['correlationId'])}, Policy = {sql_server_text(values['policy'])},\n"
                f"        Method = {sql_server_text(values['method'])}, Path = {sql_server_text(values['path'])},\n"
                f"        StatusCode = {int(document.get('statusCode') or 0)}, Outcome = {sql_server_text(values['outcome'])},\n"
                f"        ErrorCode = {error_sql}, DurationMilliseconds = {int(document.get('durationMilliseconds') or 0)},\n"
                f"        OccurredAtUtc = {datetime2(document['occurredAtUtc'])}\n"
                f"    WHERE ID = {sql_server_text(audit_id)};\n"
                "    IF @@ROWCOUNT <> 1 THROW 52011, N'Agent operation audit source row was not found.', 1;\n"
                f"    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentOperationAuditNormalizationCheckpoint WHERE ID = {sql_server_text(audit_id)})\n"
                f"        INSERT dbo.AgAgentOperationAuditNormalizationCheckpoint(ID) VALUES ({sql_server_text(audit_id)});\n\n"
            )
            if (index + 1) % BATCH_SIZE == 0 or index + 1 == len(rows):
                script.write("    COMMIT TRANSACTION;\nEND TRY\nBEGIN CATCH\n")
                script.write("    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;\n    THROW;\nEND CATCH;\nGO\n\n")
    print(f"Created: {output}")
    print(f"Source rows: {len(rows)}")
    print(f"Source row-set SHA-256: {digest.hexdigest()}")


if __name__ == "__main__":
    main()
