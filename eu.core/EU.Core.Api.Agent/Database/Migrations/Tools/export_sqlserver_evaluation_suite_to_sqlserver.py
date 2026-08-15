#!/usr/bin/env python3
"""Generate normalized Evaluation Suite SQL from current SQL Server rows."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import uuid
from pathlib import Path
from typing import Any

from export_sqlite_to_sqlserver import sql_server_text


SUITE_STATUS_BY_ORDINAL = {0: "Active", 1: "Archived"}
RUN_STATUS_BY_ORDINAL = {
    0: "Pending",
    1: "Running",
    2: "WaitingForApproval",
    3: "Completed",
    4: "Failed",
    5: "Cancelled",
    6: "Blocked",
}
RULE_GROUPS = (
    ("outputContains", "OutputContains"),
    ("outputExcludes", "OutputExcludes"),
    ("requiredEventKinds", "RequiredEventKind"),
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("output", type=Path, help="Destination Evaluation Suite normalization SQL")
    parser.add_argument(
        "--connection-env",
        default="EVALUATION_SUITE_MIGRATION_SQLSERVER_ODBC",
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


def optional_enum(value: Any, names: dict[int, str], field: str) -> str | None:
    return None if value is None or str(value).strip() == "" else enum_name(value, names, field)


def stable_id(parent_id: str, value: str) -> str:
    return str(uuid.uuid5(uuid.UUID(parent_id), value))


def load_source_rows(connection_string: str) -> list[tuple[str, str, str, int, str]]:
    try:
        import pyodbc
    except ImportError as error:
        raise RuntimeError("pyodbc is required: py -3 -m pip install pyodbc") from error

    with pyodbc.connect(connection_string, autocommit=True) as connection:
        cursor = connection.cursor()
        cursor.execute(
            "SELECT ID, TenantId, Code, LogicalRevision, DocumentJson "
            "FROM dbo.AgEvaluationSuite WHERE DocumentJson IS NOT NULL "
            "ORDER BY TenantId, Code, ID"
        )
        return [
            (str(row[0]), str(row[1]), str(row[2]), int(row[3]), str(row[4]))
            for row in cursor.fetchall()
        ]


def source_hash(rows: list[tuple[str, str, str, int, str]]) -> str:
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
            "        THROW 51513, N'Evaluation Suite text cannot be represented by VARCHAR "
            "under the current database collation.', 1;\n"
        )


def datetime2(value: Any) -> str:
    literal = sql_server_text(str(value))
    return f"CONVERT(datetime2(7), CONVERT(datetimeoffset(7), {literal}, 127))"


def insert_cases(
    script: Any,
    suite_id: str,
    version_id: str,
    cases: list[dict[str, Any]],
) -> None:
    for case_ordinal, case in enumerate(cases):
        case_row_id = stable_id(version_id, f"case:{case_ordinal}")
        specification = dict(case["specification"])
        expected_status = optional_enum(
            specification.get("expectedStatus"), RUN_STATUS_BY_ORDINAL, "ExpectedStatus"
        )
        script.write(
            "    INSERT INTO dbo.AgEvaluationCase "
            "(ID, SuiteId, VersionId, Ordinal, CaseId, Name, Input, TargetAgentId, "
            "TargetAgentVersionId, ExpectedStatus, MaximumToolCalls, MaximumDurationMilliseconds)\n"
            f"    VALUES ({guid(case_row_id)}, {guid(suite_id)}, {guid(version_id)}, "
            f"{case_ordinal}, {guid(case['id'])}, {sql_server_text(str(case.get('name') or ''))}, "
            f"{sql_server_text(str(case.get('input') or ''))}, {guid(case['targetAgentId'])}, "
            f"{guid(case['targetAgentVersionId'])}, "
            f"{('NULL' if expected_status is None else sql_server_text(expected_status))}, "
            f"{('NULL' if specification.get('maximumToolCalls') is None else int(specification['maximumToolCalls']))}, "
            f"{('NULL' if specification.get('maximumDurationMilliseconds') is None else int(specification['maximumDurationMilliseconds']))});\n"
        )
        for source_name, rule_type in RULE_GROUPS:
            for rule_ordinal, rule_value in enumerate(specification.get(source_name) or []):
                rule_id = stable_id(case_row_id, f"{rule_type}:{rule_ordinal}")
                script.write(
                    "    INSERT INTO dbo.AgEvaluationCaseRule "
                    "(ID, SuiteId, VersionId, EvaluationCaseId, RuleType, Ordinal, Value)\n"
                    f"    VALUES ({guid(rule_id)}, {guid(suite_id)}, {guid(version_id)}, "
                    f"{guid(case_row_id)}, {sql_server_text(rule_type)}, {rule_ordinal}, "
                    f"{sql_server_text(str(rule_value))});\n"
                )


def insert_version(
    script: Any,
    suite_id: str,
    version_id: str,
    ordinal: int,
    is_draft: bool,
    label: str,
    content_sha256: str,
    published_at: Any,
    published_by: str,
    cases: list[dict[str, Any]],
) -> None:
    script.write(
        "    INSERT INTO dbo.AgEvaluationSuiteVersion "
        "(ID, SuiteId, Ordinal, Label, IsDraft, ContentSha256, PublishedAtUtc, PublishedByUserId)\n"
        f"    VALUES ({guid(version_id)}, {guid(suite_id)}, {ordinal}, {sql_server_text(label)}, "
        f"{1 if is_draft else 0}, {sql_server_text(content_sha256)}, "
        f"{('NULL' if published_at is None else datetime2(published_at))}, "
        f"{sql_server_text(published_by)});\n"
    )
    insert_cases(script, suite_id, version_id, cases)


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
        script.write("-- Normalize Evaluation Suites exported from current SQL Server data.\n")
        script.write(f"-- Source row-set SHA-256: {snapshot_hash}\n")
        script.write("-- Run 025 and 026 first, then this script, then Data/027.\n\n")
        script.write("SET NOCOUNT ON;\nSET XACT_ABORT ON;\n\n")
        script.write("IF COL_LENGTH(N'dbo.AgEvaluationSuite', N'DocumentJson') IS NULL\n")
        script.write("    THROW 51511, N'DocumentJson is absent; Evaluation Suite cutover was already finalized.', 1;\n\n")
        script.write("BEGIN TRY\n    BEGIN TRANSACTION;\n\n")
        script.write("    IF OBJECT_ID(N'dbo.AgEvaluationSuiteNormalizationCheckpoint', N'U') IS NULL\n")
        script.write("        CREATE TABLE dbo.AgEvaluationSuiteNormalizationCheckpoint (SuiteId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY);\n\n")

        for row_id, tenant_id, code, logical_revision, document_json in rows:
            document = json.loads(document_json)
            if str(document.get("id", "")).casefold() != row_id.casefold():
                raise ValueError(f"Evaluation Suite {row_id} has a mismatched document id")
            if str(document.get("tenantId", "")) != tenant_id or str(document.get("code", "")) != code:
                raise ValueError(f"Evaluation Suite {row_id} has a mismatched tenant or code")
            if int(document.get("logicalRevision", logical_revision)) != logical_revision:
                raise ValueError(f"Evaluation Suite {row_id} has a mismatched logical revision")

            script.write(f"    -- Evaluation Suite {row_id}\n")
            write_varchar_guards(script, strings_in(document))
            script.write(
                "    UPDATE dbo.AgEvaluationSuite SET\n"
                f"        Name = {sql_server_text(str(document.get('name') or ''))},\n"
                f"        Description = {sql_server_text(str(document.get('description') or ''))},\n"
                f"        Status = {sql_server_text(enum_name(document.get('status', 0), SUITE_STATUS_BY_ORDINAL, 'Status'))},\n"
                f"        CreatedAtUtc = {datetime2(document['createdAtUtc'])},\n"
                f"        UpdatedAtUtc = {datetime2(document['updatedAtUtc'])},\n"
                f"        CreatedByUserId = {sql_server_text(str(document.get('createdBy') or ''))},\n"
                f"        UpdatedByUserId = {sql_server_text(str(document.get('updatedBy') or ''))}\n"
                f"    WHERE ID = {guid(row_id)} AND TenantId = {sql_server_text(tenant_id)} "
                f"AND Code = {sql_server_text(code)} AND LogicalRevision = {logical_revision};\n"
                "    IF @@ROWCOUNT <> 1 THROW 51512, N'Evaluation Suite source row was not found.', 1;\n"
            )
            script.write(
                "    DELETE caseRule FROM dbo.AgEvaluationCaseRule AS caseRule WHERE caseRule.SuiteId = " + guid(row_id) + ";\n"
                "    DELETE evaluationCase FROM dbo.AgEvaluationCase evaluationCase WHERE evaluationCase.SuiteId = " + guid(row_id) + ";\n"
                "    DELETE version FROM dbo.AgEvaluationSuiteVersion version WHERE version.SuiteId = " + guid(row_id) + ";\n"
            )

            draft_id = stable_id(row_id, "draft-version")
            draft = dict(document.get("draft") or {})
            insert_version(
                script, row_id, draft_id, 0, True, "draft", "", None, "",
                list(draft.get("cases") or []),
            )
            for ordinal, version_value in enumerate(document.get("publishedVersions") or [], start=1):
                version = dict(version_value)
                version_id = str(version["id"])
                insert_version(
                    script, row_id, version_id, ordinal, False,
                    str(version.get("label") or ""),
                    str(version.get("contentSha256") or ""),
                    version["publishedAtUtc"],
                    str(version.get("publishedBy") or ""),
                    list(version.get("cases") or []),
                )
            script.write(
                "    IF NOT EXISTS (SELECT 1 FROM dbo.AgEvaluationSuiteNormalizationCheckpoint "
                f"WHERE SuiteId = {guid(row_id)})\n"
                "        INSERT INTO dbo.AgEvaluationSuiteNormalizationCheckpoint (SuiteId) "
                f"VALUES ({guid(row_id)});\n\n"
            )

        script.write("    COMMIT TRANSACTION;\nEND TRY\nBEGIN CATCH\n")
        script.write("    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;\n    THROW;\nEND CATCH;\n")

    print(f"Created: {output}")
    print(f"Source rows: {len(rows)}")
    print(f"Source row-set SHA-256: {snapshot_hash}")


if __name__ == "__main__":
    main()
