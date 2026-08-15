#!/usr/bin/env python3
"""Generate normalized Evaluation Batch SQL from current SQL Server rows."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import uuid
from decimal import Decimal, InvalidOperation
from pathlib import Path
from typing import Any

from export_sqlite_to_sqlserver import sql_server_text


BATCH_STATUS_BY_ORDINAL = {0: "Running", 1: "Completed", 2: "Cancelled", 3: "Failed"}
CASE_STATUS_BY_ORDINAL = {0: "Pending", 1: "Running", 2: "Passed", 3: "Failed", 4: "Cancelled"}
RUN_STATUS_BY_ORDINAL = {
    0: "Pending",
    1: "Running",
    2: "WaitingForApproval",
    3: "Completed",
    4: "Failed",
    5: "Cancelled",
    6: "Blocked",
}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("output", type=Path, help="Destination Evaluation Batch normalization SQL")
    parser.add_argument(
        "--connection-env",
        default="EVALUATION_BATCH_MIGRATION_SQLSERVER_ODBC",
        help="Environment variable containing an ODBC SQL Server connection string",
    )
    return parser.parse_args()


def guid(value: object) -> str:
    return f"CONVERT(uniqueidentifier, {sql_server_text(str(value))})"


def optional_guid(value: Any) -> str:
    return "NULL" if value is None or str(value).strip() == "" else guid(value)


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


def optional_enum(value: Any, names: dict[int, str], field: str) -> str:
    return "" if value is None or str(value).strip() == "" else enum_name(value, names, field)


def boolean(value: Any, field: str) -> bool:
    if isinstance(value, bool):
        return value
    raise ValueError(f"{field} {value!r} is invalid")


def decimal_literal(value: Any, field: str) -> str:
    try:
        result = Decimal(str(value))
    except (InvalidOperation, ValueError) as error:
        raise ValueError(f"{field} {value!r} is invalid") from error
    if not result.is_finite():
        raise ValueError(f"{field} {value!r} is invalid")
    return format(result, "f")


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
            "        THROW 51613, N'Evaluation Batch text cannot be represented by VARCHAR "
            "under the current database collation.', 1;\n"
        )


def load_source_rows(connection_string: str) -> list[tuple[str, str, str, str, str, int, str, str]]:
    try:
        import pyodbc
    except ImportError as error:
        raise RuntimeError("pyodbc is required: py -3 -m pip install pyodbc") from error

    with pyodbc.connect(connection_string, autocommit=True) as connection:
        cursor = connection.cursor()
        cursor.execute(
            "SELECT ID, TenantId, SuiteId, SuiteVersionId, Status, LogicalRevision, "
            "StartedAtUtc, DocumentJson FROM dbo.AgEvaluationBatch "
            "WHERE DocumentJson IS NOT NULL ORDER BY TenantId, StartedAtUtc, ID"
        )
        return [
            (
                str(row[0]), str(row[1]), str(row[2]), str(row[3]), str(row[4]),
                int(row[5]), str(row[6]), str(row[7]),
            )
            for row in cursor.fetchall()
        ]


def source_hash(rows: list[tuple[str, str, str, str, str, int, str, str]]) -> str:
    digest = hashlib.sha256()
    for row in rows:
        digest.update(json.dumps(row, ensure_ascii=False, separators=(",", ":")).encode("utf-8"))
        digest.update(b"\n")
    return digest.hexdigest()


def insert_checks(script: Any, batch_id: str, case_row_id: str, checks: list[dict[str, Any]]) -> None:
    for ordinal, check in enumerate(checks):
        row_id = stable_id(case_row_id, f"check:{ordinal}")
        script.write(
            "    INSERT INTO dbo.AgEvaluationBatchCheck "
            "(ID, BatchId, BatchCaseId, Ordinal, Code, Passed, Expected, Actual)\n"
            f"    VALUES ({guid(row_id)}, {guid(batch_id)}, {guid(case_row_id)}, {ordinal}, "
            f"{sql_server_text(str(check.get('code') or ''))}, {1 if boolean(check.get('passed'), 'Check.Passed') else 0}, "
            f"{sql_server_text(str(check.get('expected') or ''))}, "
            f"{sql_server_text(str(check.get('actual') or ''))});\n"
        )


def insert_observations(
    script: Any,
    batch_id: str,
    case_row_id: str,
    observation_type: str,
    values: list[Any],
) -> None:
    for ordinal, value in enumerate(values):
        row_id = stable_id(case_row_id, f"observation:{observation_type}:{ordinal}")
        script.write(
            "    INSERT INTO dbo.AgEvaluationBatchObservation "
            "(ID, BatchId, BatchCaseId, ObservationType, Ordinal, Value)\n"
            f"    VALUES ({guid(row_id)}, {guid(batch_id)}, {guid(case_row_id)}, "
            f"{sql_server_text(observation_type)}, {ordinal}, {sql_server_text(str(value))});\n"
        )


def insert_cases(script: Any, batch_id: str, cases: list[dict[str, Any]]) -> None:
    for ordinal, case in enumerate(cases):
        case_row_id = stable_id(batch_id, f"case:{ordinal}")
        report_value = case.get("report")
        report = dict(report_value) if isinstance(report_value, dict) else None
        unified_run_status = optional_enum(
            case.get("unifiedRunStatus"), RUN_STATUS_BY_ORDINAL, "UnifiedRunStatus"
        )
        script.write(
            "    INSERT INTO dbo.AgEvaluationBatchCase "
            "(ID, BatchId, Ordinal, CaseId, CaseName, TargetAgentId, TargetAgentVersionId, "
            "Status, UnifiedRunId, UnifiedRunStatus, ErrorCode, DurationMilliseconds, "
            "ToolCallCount, ReportEvaluatedAtUtc, ReportPassed, ReportScore, OutputSha256, "
            "OutputUtf8Bytes)\n"
            f"    VALUES ({guid(case_row_id)}, {guid(batch_id)}, {ordinal}, {guid(case['caseId'])}, "
            f"{sql_server_text(str(case.get('caseName') or ''))}, {guid(case['targetAgentId'])}, "
            f"{guid(case['targetAgentVersionId'])}, "
            f"{sql_server_text(enum_name(case.get('status'), CASE_STATUS_BY_ORDINAL, 'CaseStatus'))}, "
            f"{optional_guid(case.get('unifiedRunId'))}, {sql_server_text(unified_run_status)}, "
            f"{sql_server_text(str(case.get('errorCode') or ''))}, "
            f"{('NULL' if case.get('durationMilliseconds') is None else int(case['durationMilliseconds']))}, "
            f"{int(case.get('toolCallCount') or 0)}, "
            f"{('NULL' if report is None else datetime2(report['evaluatedAtUtc']))}, "
            f"{('NULL' if report is None else (1 if boolean(report.get('passed'), 'Report.Passed') else 0))}, "
            f"{('NULL' if report is None else decimal_literal(report.get('score'), 'Report.Score'))}, "
            f"{sql_server_text('' if report is None else str(report.get('outputSha256') or ''))}, "
            f"{('NULL' if report is None else int(report.get('outputUtf8Bytes') or 0))});\n"
        )
        if report is not None:
            insert_checks(script, batch_id, case_row_id, list(report.get("checks") or []))
        insert_observations(
            script, batch_id, case_row_id, "EventKind", list(case.get("observedEventKinds") or [])
        )
        insert_observations(
            script, batch_id, case_row_id, "Route", list(case.get("observedRoutes") or [])
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
        script.write("-- Normalize Evaluation Batches exported from current SQL Server data.\n")
        script.write(f"-- Source row-set SHA-256: {snapshot_hash}\n")
        script.write("-- Run 030 and 031 first, then this script, then Data/032.\n\n")
        script.write("SET NOCOUNT ON;\nSET XACT_ABORT ON;\n\n")
        script.write("IF COL_LENGTH(N'dbo.AgEvaluationBatch', N'DocumentJson') IS NULL\n")
        script.write("    THROW 51611, N'DocumentJson is absent; Evaluation Batch cutover was already finalized.', 1;\n\n")
        script.write("BEGIN TRY\n    BEGIN TRANSACTION;\n\n")
        script.write("    IF OBJECT_ID(N'dbo.AgEvaluationBatchNormalizationCheckpoint', N'U') IS NULL\n")
        script.write("        CREATE TABLE dbo.AgEvaluationBatchNormalizationCheckpoint (BatchId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY);\n\n")

        for row_id, tenant_id, suite_id, version_id, status, revision, _, document_json in rows:
            document = json.loads(document_json)
            if str(document.get("id", "")).casefold() != row_id.casefold():
                raise ValueError(f"Evaluation Batch {row_id} has a mismatched document id")
            if str(document.get("tenantId", "")) != tenant_id:
                raise ValueError(f"Evaluation Batch {row_id} has a mismatched tenant")
            if str(document.get("suiteId", "")).casefold() != suite_id.casefold():
                raise ValueError(f"Evaluation Batch {row_id} has a mismatched suite id")
            if str(document.get("suiteVersionId", "")).casefold() != version_id.casefold():
                raise ValueError(f"Evaluation Batch {row_id} has a mismatched suite version id")
            if int(document.get("logicalRevision", revision)) != revision:
                raise ValueError(f"Evaluation Batch {row_id} has a mismatched logical revision")
            if enum_name(document.get("status"), BATCH_STATUS_BY_ORDINAL, "Status") != enum_name(
                status, BATCH_STATUS_BY_ORDINAL, "StoredStatus"
            ):
                raise ValueError(f"Evaluation Batch {row_id} has a mismatched status")

            script.write(f"    -- Evaluation Batch {row_id}\n")
            write_varchar_guards(script, strings_in(document))
            script.write(
                "    UPDATE dbo.AgEvaluationBatch SET\n"
                f"        RequestedByUserId = {sql_server_text(str(document.get('requestedBy') or ''))},\n"
                f"        SuiteVersionContentSha256 = {sql_server_text(str(document.get('suiteVersionContentSha256') or ''))},\n"
                f"        Status = {sql_server_text(enum_name(document.get('status'), BATCH_STATUS_BY_ORDINAL, 'Status'))},\n"
                f"        StartedAtUtc = {datetime2(document['startedAtUtc'])},\n"
                f"        FinishedAtUtc = {optional_datetime2(document.get('finishedAtUtc'))},\n"
                f"        ErrorCode = {sql_server_text(str(document.get('errorCode') or ''))}\n"
                f"    WHERE ID = {guid(row_id)} AND TenantId = {sql_server_text(tenant_id)} "
                f"AND SuiteId = {guid(suite_id)} AND SuiteVersionId = {guid(version_id)} "
                f"AND LogicalRevision = {revision};\n"
                "    IF @@ROWCOUNT <> 1 THROW 51612, N'Evaluation Batch source row was not found.', 1;\n"
            )
            script.write(
                "    DELETE batchCheck FROM dbo.AgEvaluationBatchCheck AS batchCheck WHERE batchCheck.BatchId = " + guid(row_id) + ";\n"
                "    DELETE batchObservation FROM dbo.AgEvaluationBatchObservation AS batchObservation WHERE batchObservation.BatchId = " + guid(row_id) + ";\n"
                "    DELETE batchCase FROM dbo.AgEvaluationBatchCase AS batchCase WHERE batchCase.BatchId = " + guid(row_id) + ";\n"
            )
            insert_cases(script, row_id, list(document.get("cases") or []))
            script.write(
                "    IF NOT EXISTS (SELECT 1 FROM dbo.AgEvaluationBatchNormalizationCheckpoint "
                f"WHERE BatchId = {guid(row_id)})\n"
                "        INSERT INTO dbo.AgEvaluationBatchNormalizationCheckpoint (BatchId) "
                f"VALUES ({guid(row_id)});\n\n"
            )

        script.write("    COMMIT TRANSACTION;\nEND TRY\nBEGIN CATCH\n")
        script.write("    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;\n    THROW;\nEND CATCH;\n")

    print(f"Created: {output}")
    print(f"Source rows: {len(rows)}")
    print(f"Source row-set SHA-256: {snapshot_hash}")


if __name__ == "__main__":
    main()
