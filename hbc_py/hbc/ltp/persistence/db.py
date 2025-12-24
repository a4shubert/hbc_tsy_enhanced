import logging
import sqlite3
from pathlib import Path
from typing import Any, Iterable, Optional

import pandas as pd


class SqlLiteDataBase:
    """Lightweight helper for executing raw SQLite queries.
       Not to be used in production - just for prototyping
    """

    def __init__(self, db_path: Optional[str | Path] = None):
        """
        Establish a connection to the SQLite file (created if missing).

        Parameters
        ----------
        db_path : Optional[str | Path]
            Path to the SQLite database file. Defaults to `./hbc_db/hbc.sqlite3`
            relative to the current working directory.
        """
        default_path = Path.cwd() / "hbc_db" / "hbc.sqlite3"
        self.db_path = Path(db_path) if db_path is not None else default_path
        self.db_path.parent.mkdir(parents=True, exist_ok=True)
        self.conn = sqlite3.connect(self.db_path)
        self.conn.row_factory = sqlite3.Row
        self.logger = logging.getLogger()

    @property
    def all_dbs(self) -> list[str]:
        """List attached databases as strings 'name:file'."""
        df = self.run_query("PRAGMA database_list;")
        return [f"{row['name']}:{row['file']}" for _, row in df.iterrows()]

    @property
    def all_tables(self) -> list[str]:
        """
        List tables in the current database as `db_name:table_name`.
        """
        db_name = "main"
        df = self.run_query(
            "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;"
        )
        if df.empty:
            return []
        return [f"{db_name}:{name}" for name in df["name"].tolist()]

    def run_query(self, query: str, params: Optional[Iterable[Any]] = None):
        """
        Execute a native SQLite query and return the result.

        - For SELECT-like statements (cursor has a description), returns a pandas DataFrame.
        - For non-SELECT statements, commits and returns the affected rowcount.
        """
        cur = self.conn.cursor()
        cur.execute(query, params or [])
        if cur.description:
            rows = cur.fetchall()
            cols = [desc[0] for desc in cur.description]
            return pd.DataFrame(rows, columns=cols)
        self.conn.commit()
        return cur.rowcount

    def create_table_from_df(self, table_name: str, df: pd.DataFrame) -> None:
        """
        Create/replace a table from a DataFrame, inferring SQLite column types.

        - Drops any existing table of the same name.
        - Commits inserts on success; logs outcome.
        """
        if df.empty:
            self.logger.warning(
                "DataFrame is empty; skipping table creation for %s", table_name
            )
            return

        type_map = {
            "int64": "INTEGER",
            "int32": "INTEGER",
            "float64": "REAL",
            "float32": "REAL",
            "bool": "INTEGER",
            
            "datetime64[ns]": "TEXT",
        }

        col_defs = []
        for col, dtype in df.dtypes.items():
            sqlite_type = type_map.get(str(dtype), "TEXT")
            col_defs.append(f'"{col}" {sqlite_type}')
        create_sql = (
            f'DROP TABLE IF EXISTS "{table_name}";\n'
            f'CREATE TABLE "{table_name}" ({", ".join(col_defs)});'
        )

        try:
            cur = self.conn.cursor()
            cur.executescript(create_sql)

            placeholders = ", ".join(["?"] * len(df.columns))
            insert_sql = (
                f'INSERT INTO "{table_name}" ({", ".join([f"{c}" for c in df.columns])}) '
                f"VALUES ({placeholders})"
            )
            cur.executemany(insert_sql, df.itertuples(index=False, name=None))
            self.conn.commit()
            self.logger.info(
                "Created table %s with %s rows", table_name, len(df)
            )
        except Exception as exc:
            self.conn.rollback()
            self.logger.error(
                "Failed to create table %s: %s", table_name, exc, exc_info=True
            )
            raise

    def execute(self, command: str, params: Optional[Iterable[Any]] = None) -> int:
        """
        Execute a non-SELECT SQL command; returns affected rowcount.
        """
        cur = self.conn.cursor()
        cur.execute(command, params or [])
        self.conn.commit()
        return cur.rowcount

    def close(self) -> None:
        """Close the open SQLite connection."""
        try:
            self.conn.close()
        except Exception:
            pass
