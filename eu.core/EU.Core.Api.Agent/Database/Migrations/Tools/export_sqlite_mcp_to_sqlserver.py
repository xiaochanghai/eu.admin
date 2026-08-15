#!/usr/bin/env python3
"""Generate SQL Server MCP normalization statements from a read-only SQLite snapshot."""

from __future__ import annotations

import argparse
import sqlite3
from contextlib import closing
from pathlib import Path

from export_sqlite_to_sqlserver import (
    create_snapshot,
    sha256_file,
    write_mcp_normalization_script,
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("source", type=Path, help="Path to eu-core-agent.db")
    parser.add_argument("output", type=Path, help="Destination MCP normalization .sql file")
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    source = args.source.resolve()
    output = args.output.resolve()
    if not source.is_file():
        raise FileNotFoundError(source)
    if source == output:
        raise ValueError("Source SQLite database and SQL output must be different files")

    output.parent.mkdir(parents=True, exist_ok=True)
    snapshot = create_snapshot(source, output.parent)
    try:
        snapshot_hash = sha256_file(snapshot)
        with closing(sqlite3.connect(snapshot)) as connection:
            violations = connection.execute("PRAGMA foreign_key_check").fetchall()
            if violations:
                raise RuntimeError(
                    f"SQLite foreign-key check failed with {len(violations)} violation(s)"
                )
            exists = connection.execute(
                "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'mcp_server_definitions'"
            ).fetchone()
            if exists is None:
                raise RuntimeError("SQLite table mcp_server_definitions is missing")
            write_mcp_normalization_script(
                connection,
                output,
                source,
                snapshot_hash,
            )

        print(f"Created: {output}")
        print(f"Snapshot SHA-256: {snapshot_hash}")
    finally:
        snapshot.unlink(missing_ok=True)


if __name__ == "__main__":
    main()
