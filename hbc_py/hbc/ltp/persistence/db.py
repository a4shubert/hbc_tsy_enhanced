import logging
import os
from pathlib import Path
from typing import Any, Mapping, Optional

import pandas as pd
from sqlalchemy import create_engine, inspect, text
from sqlalchemy.engine import Engine


class SqlLiteDataBase:
    """
    Lightweight helper built on SQLAlchemy for executing SQLite queries.
    Keeps the same surface as the previous sqlite3 wrapper but gains
    connection pooling and richer introspection.
    """

    def __init__(self, db_path: Optional[str | Path] = None, echo: bool = False):
        """
        Establish an engine to the SQLite file (created if missing).

        Parameters
        ----------
        db_path : Optional[str | Path]
            Path to the SQLite database file. Defaults to env HBC_DB_PATH
            or `<repo>/hbc_db/hbc.db`.
        echo : bool
            Whether to enable SQLAlchemy echo logging for debugging.
        """
        env_path = os.environ.get("HBC_DB_PATH")
        if env_path:
            default_path = Path(env_path)
        else:
            repo_root = Path(__file__).resolve().parents[4]
            default_path = repo_root / "hbc_db" / "hbc.db"
        self.db_path = Path(db_path) if db_path is not None else default_path
        self.db_path.parent.mkdir(parents=True, exist_ok=True)

        self.engine: Engine = create_engine(
            f"sqlite:///{self.db_path}", future=True, echo=echo
        )
        self.logger = logging.getLogger(__name__)

    @property
    def all_dbs(self) -> list[str]:
        """List attached databases as strings 'name:file'."""
        df = self.run_query("PRAGMA database_list;")
        return [f"{row['name']}:{row['file']}" for _, row in df.iterrows()]

    @property
    def all_tables(self) -> list[str]:
        """List user tables in the current database."""
        insp = inspect(self.engine)
        names = insp.get_table_names()
        filtered = [
            n
            for n in names
            if not n.startswith("sqlite_") and n != "__EFMigrationsHistory"
        ]
        return sorted(filtered)

    def run_query(
        self, query: str, params: Optional[Mapping[str, Any] | Mapping] = None
    ):
        """
        Execute SQL and return a DataFrame for SELECT-like results or rowcount for DML.
        """
        with self.engine.begin() as conn:
            result = conn.execute(text(query), params or {})
            if result.returns_rows:
                rows = result.fetchall()
                cols = result.keys()
                return pd.DataFrame(rows, columns=cols)
            return result.rowcount

    def execute(
        self, command: str, params: Optional[Mapping[str, Any] | Mapping] = None
    ) -> int:
        """Execute a non-SELECT SQL command; returns affected rowcount."""
        res = self.run_query(command, params=params)
        return int(res if res is not None else 0)

    def close(self) -> None:
        """Dispose the SQLAlchemy engine."""
        try:
            self.engine.dispose()
        except Exception:
            pass
