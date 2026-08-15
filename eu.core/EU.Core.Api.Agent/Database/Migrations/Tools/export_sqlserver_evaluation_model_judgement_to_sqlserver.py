#!/usr/bin/env python3
"""Generate normalized Evaluation Model Judgement SQL from current SQL Server rows."""

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


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("output", type=Path, help="Destination normalization SQL")
    parser.add_argument(
        "--connection-env",
        default="EVALUATION_MODEL_JUDGEMENT_MIGRATION_SQLSERVER_ODBC",
        help="Environment variable containing an ODBC SQL Server connection string",
    )
    return parser.parse_args()


def guid(value: object) -> str:
    return f"CONVERT(uniqueidentifier, {sql_server_text(str(value))})"


def stable_id(parent_id: str, value: str) -> str:
    return str(uuid.uuid5(uuid.UUID(parent_id), value))


def datetime2(value: Any) -> str:
    literal = sql_server_text(str(value))
    return f"CONVERT(datetime2(7), CONVERT(datetimeoffset(7), {literal}, 127))"


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


def optional_decimal_literal(value: Any, field: str) -> str:
    return "NULL" if value is None else decimal_literal(value, field)


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
            "        THROW 51713, N'Evaluation Model Judgement text cannot be represented "
            "by VARCHAR under the current database collation.', 1;\n"
        )


def load_source_rows(connection_string: str) -> list[tuple[str, str, str, str, str, str]]:
    try:
        import pyodbc
    except ImportError as error:
        raise RuntimeError("pyodbc is required: py -3 -m pip install pyodbc") from error

    with pyodbc.connect(connection_string, autocommit=True) as connection:
        cursor = connection.cursor()
        cursor.execute(
            "SELECT ID, TenantId, BatchId, ConfigurationSha256, StartedAtUtc, DocumentJson "
            "FROM dbo.AgEvaluationModelJudgement WHERE DocumentJson IS NOT NULL "
            "ORDER BY TenantId, StartedAtUtc, ID"
        )
        return [tuple(str(item) for item in row) for row in cursor.fetchall()]


def source_hash(rows: list[tuple[str, str, str, str, str, str]]) -> str:
    digest = hashlib.sha256()
    for row in rows:
        digest.update(json.dumps(row, ensure_ascii=False, separators=(",", ":")).encode("utf-8"))
        digest.update(b"\n")
    return digest.hexdigest()


def insert_evaluators(script: Any, judgement_id: str, values: list[Any]) -> None:
    for ordinal, value in enumerate(values):
        row_id = stable_id(judgement_id, f"evaluator:{ordinal}")
        script.write(
            "    INSERT INTO dbo.AgEvaluationModelJudgementEvaluator "
            "(ID, JudgementId, Ordinal, Name)\n"
            f"    VALUES ({guid(row_id)}, {guid(judgement_id)}, {ordinal}, "
            f"{sql_server_text(str(value))});\n"
        )


def insert_minimum_scores(script: Any, judgement_id: str, values: dict[str, Any]) -> None:
    for ordinal, (name, score) in enumerate(values.items()):
        row_id = stable_id(judgement_id, f"minimum-score:{ordinal}")
        script.write(
            "    INSERT INTO dbo.AgEvaluationModelJudgementMinimumScore "
            "(ID, JudgementId, Ordinal, Name, Score)\n"
            f"    VALUES ({guid(row_id)}, {guid(judgement_id)}, {ordinal}, "
            f"{sql_server_text(str(name))}, {decimal_literal(score, 'MinimumScore.Score')});\n"
        )


def insert_diagnostics(
    script: Any,
    judgement_id: str,
    metric_row_id: str,
    values: list[Any],
) -> None:
    for ordinal, value in enumerate(values):
        row_id = stable_id(metric_row_id, f"diagnostic:{ordinal}")
        script.write(
            "    INSERT INTO dbo.AgEvaluationModelJudgementDiagnostic "
            "(ID, JudgementId, JudgementMetricId, Ordinal, Code)\n"
            f"    VALUES ({guid(row_id)}, {guid(judgement_id)}, {guid(metric_row_id)}, "
            f"{ordinal}, {sql_server_text(str(value))});\n"
        )


def insert_metrics(
    script: Any,
    judgement_id: str,
    case_row_id: str,
    values: list[dict[str, Any]],
) -> None:
    for ordinal, value in enumerate(values):
        row_id = stable_id(case_row_id, f"metric:{ordinal}")
        script.write(
            "    INSERT INTO dbo.AgEvaluationModelJudgementMetric "
            "(ID, JudgementId, JudgementCaseId, Ordinal, Name, Score, MinimumScore, Passed)\n"
            f"    VALUES ({guid(row_id)}, {guid(judgement_id)}, {guid(case_row_id)}, "
            f"{ordinal}, {sql_server_text(str(value.get('name') or ''))}, "
            f"{optional_decimal_literal(value.get('score'), 'Metric.Score')}, "
            f"{decimal_literal(value.get('minimumScore'), 'Metric.MinimumScore')}, "
            f"{1 if boolean(value.get('passed'), 'Metric.Passed') else 0});\n"
        )
        insert_diagnostics(
            script,
            judgement_id,
            row_id,
            list(value.get("diagnosticCodes") or []),
        )


def insert_cases(script: Any, judgement_id: str, values: list[dict[str, Any]]) -> None:
    for ordinal, value in enumerate(values):
        row_id = stable_id(judgement_id, f"case:{ordinal}")
        script.write(
            "    INSERT INTO dbo.AgEvaluationModelJudgementCase "
            "(ID, JudgementId, Ordinal, CaseId, CaseName, UnifiedRunId, InputSha256, OutputSha256)\n"
            f"    VALUES ({guid(row_id)}, {guid(judgement_id)}, {ordinal}, "
            f"{guid(value['caseId'])}, {sql_server_text(str(value.get('caseName') or ''))}, "
            f"{guid(value['unifiedRunId'])}, {sql_server_text(str(value.get('inputSha256') or ''))}, "
            f"{sql_server_text(str(value.get('outputSha256') or ''))});\n"
        )
        insert_metrics(script, judgement_id, row_id, list(value.get("metrics") or []))


def validate_source(
    row: tuple[str, str, str, str, str, str], document: dict[str, Any]
) -> None:
    row_id, tenant_id, batch_id, configuration_sha256, _, _ = row
    comparisons = (
        ("id", row_id),
        ("tenantId", tenant_id),
        ("batchId", batch_id),
        ("configurationSha256", configuration_sha256),
    )
    for field, expected in comparisons:
        actual = str(document.get(field, ""))
        if field in {"id", "batchId"}:
            matches = actual.casefold() == expected.casefold()
        else:
            matches = actual == expected
        if not matches:
            raise ValueError(f"Evaluation Model Judgement {row_id} has a mismatched {field}")


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
        script.write("-- Normalize Evaluation Model Judgements exported from current SQL Server data.\n")
        script.write(f"-- Source row-set SHA-256: {snapshot_hash}\n")
        script.write("-- Run 035 and 036 first, then this script, then Data/037.\n\n")
        script.write("SET NOCOUNT ON;\nSET XACT_ABORT ON;\n\n")
        script.write("IF COL_LENGTH(N'dbo.AgEvaluationModelJudgement', N'DocumentJson') IS NULL\n")
        script.write("    THROW 51711, N'DocumentJson is absent; the cutover was already finalized.', 1;\n\n")
        script.write("BEGIN TRY\n    BEGIN TRANSACTION;\n\n")
        script.write("    IF OBJECT_ID(N'dbo.AgEvaluationModelJudgementNormalizationCheckpoint', N'U') IS NULL\n")
        script.write("        CREATE TABLE dbo.AgEvaluationModelJudgementNormalizationCheckpoint (JudgementId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY);\n\n")

        for row in rows:
            row_id, tenant_id, batch_id, configuration_sha256, _, document_json = row
            document = json.loads(document_json)
            validate_source(row, document)
            script.write(f"    -- Evaluation Model Judgement {row_id}\n")
            write_varchar_guards(script, strings_in(document))
            script.write(
                "    UPDATE dbo.AgEvaluationModelJudgement SET\n"
                f"        RequestedByUserId = {sql_server_text(str(document.get('requestedBy') or ''))},\n"
                f"        SuiteId = {guid(document['suiteId'])},\n"
                f"        SuiteVersionId = {guid(document['suiteVersionId'])},\n"
                f"        SuiteVersionContentSha256 = {sql_server_text(str(document.get('suiteVersionContentSha256') or ''))},\n"
                f"        Provider = {sql_server_text(str(document.get('provider') or ''))},\n"
                f"        PackageVersion = {sql_server_text(str(document.get('packageVersion') or ''))},\n"
                f"        ModelProfileId = {sql_server_text(str(document.get('modelProfileId') or ''))},\n"
                f"        PromptVersion = {sql_server_text(str(document.get('promptVersion') or ''))},\n"
                f"        StartedAtUtc = {datetime2(document['startedAtUtc'])},\n"
                f"        FinishedAtUtc = {datetime2(document['finishedAtUtc'])},\n"
                f"        AdvisoryPassed = {1 if boolean(document.get('advisoryPassed'), 'AdvisoryPassed') else 0}\n"
                f"    WHERE ID = {guid(row_id)} AND TenantId = {sql_server_text(tenant_id)} "
                f"AND BatchId = {guid(batch_id)} AND ConfigurationSha256 = {sql_server_text(configuration_sha256)};\n"
                "    IF @@ROWCOUNT <> 1 THROW 51712, N'Evaluation Model Judgement source row was not found.', 1;\n"
            )
            script.write(
                "    DELETE diagnosticRow FROM dbo.AgEvaluationModelJudgementDiagnostic AS diagnosticRow WHERE diagnosticRow.JudgementId = " + guid(row_id) + ";\n"
                "    DELETE metricRow FROM dbo.AgEvaluationModelJudgementMetric AS metricRow WHERE metricRow.JudgementId = " + guid(row_id) + ";\n"
                "    DELETE caseRow FROM dbo.AgEvaluationModelJudgementCase AS caseRow WHERE caseRow.JudgementId = " + guid(row_id) + ";\n"
                "    DELETE scoreRow FROM dbo.AgEvaluationModelJudgementMinimumScore AS scoreRow WHERE scoreRow.JudgementId = " + guid(row_id) + ";\n"
                "    DELETE evaluatorRow FROM dbo.AgEvaluationModelJudgementEvaluator AS evaluatorRow WHERE evaluatorRow.JudgementId = " + guid(row_id) + ";\n"
            )
            insert_evaluators(script, row_id, list(document.get("evaluators") or []))
            minimum_scores = document.get("minimumScores") or {}
            if not isinstance(minimum_scores, dict):
                raise ValueError(f"Evaluation Model Judgement {row_id} minimumScores is invalid")
            insert_minimum_scores(script, row_id, minimum_scores)
            insert_cases(script, row_id, list(document.get("cases") or []))
            script.write(
                "    IF NOT EXISTS (SELECT 1 FROM dbo.AgEvaluationModelJudgementNormalizationCheckpoint "
                f"WHERE JudgementId = {guid(row_id)})\n"
                "        INSERT INTO dbo.AgEvaluationModelJudgementNormalizationCheckpoint (JudgementId) "
                f"VALUES ({guid(row_id)});\n\n"
            )

        script.write("    COMMIT TRANSACTION;\nEND TRY\nBEGIN CATCH\n")
        script.write("    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;\n    THROW;\nEND CATCH;\n")

    print(f"Created: {output}")
    print(f"Source rows: {len(rows)}")
    print(f"Source row-set SHA-256: {snapshot_hash}")


if __name__ == "__main__":
    main()
