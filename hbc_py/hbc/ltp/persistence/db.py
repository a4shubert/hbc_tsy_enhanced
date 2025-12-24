import logging
import sqlite3
import os
from pathlib import Path
from typing import Any, Iterable, Optional

import pandas as pd
import pandas.api.types as ptypes


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
            Path to the SQLite database file. Defaults to env HBC_DB_PATH
            or `<repo>/hbc_db/hbc.db`.
        """
        env_path = os.environ.get("HBC_DB_PATH")
        if env_path:
            default_path = Path(env_path)
        else:
            repo_root = Path(__file__).resolve().parents[4]
            default_path = repo_root / "hbc_db" / "hbc.db"
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

    def update_surveys_table(
        self,
        df: pd.DataFrame,
        verify: Optional[bool] = None,
    ) -> list[int]:
        """
        Push DataFrame rows to the surveys API.
        - If Id is provided, tries PUT; falls back to POST on 404.
        - If Id is missing, uses POST.

        Uses env HBC_API_URL (default http://localhost:5047) and endpoints:
        - POST /surveys
        - PUT /surveys/{id}
        """
        if df is None or df.empty:
            self.logger.warning("DataFrame is empty; nothing to sync.")
            return []
        try:
            import requests
        except ImportError as exc:
            raise ImportError("requests package is required for API sync") from exc

        api_base = os.environ.get("HBC_API_URL", "http://localhost:5047").rstrip("/")
        verify_flag = verify
        if verify_flag is None:
            env_verify = os.environ.get("HBC_API_VERIFY", "").strip().lower()
            if env_verify in {"false", "0", "no", "off"}:
                verify_flag = False
            elif env_verify in {"true", "1", "yes", "on"}:
                verify_flag = True

        data = df.copy()
        # convert datetime columns to isoformat strings
        for col in data.columns:
            if ptypes.is_datetime64_any_dtype(data[col]):
                data[col] = data[col].dt.strftime("%Y-%m-%dT%H:%M:%S")

        # Replace NaN/NaT with None for JSON compatibility.
        data = data.where(pd.notnull(data), None)

        # Add deterministic unique_key hash column to help dedupe/upsert decisions.
        if "unique_key" not in data.columns:
            import hashlib
            import json

            def _hash_row(row):
                payload = {k: row[k] for k in data.columns if k != "unique_key"}
                serialized = json.dumps(payload, sort_keys=True, default=str)
                return hashlib.sha1(serialized.encode("utf-8")).hexdigest()

            data["unique_key"] = data.apply(_hash_row, axis=1)

        # Drop duplicates on unique_key within this payload.
        data = data.drop_duplicates(subset=["unique_key"])

        records = data.to_dict(orient="records")
        if not records:
            return []

        status_codes: list[int] = []
        chunk_size = 100
        for i in range(0, len(records), chunk_size):
            batch = records[i : i + chunk_size]
            self.logger.info(
                "Posting batch %s-%s/%s to %s/surveys/batch (verify=%s)",
                i + 1,
                i + len(batch),
                len(records),
                api_base,
                verify_flag,
            )
            resp = requests.post(
                f"{api_base}/surveys/batch",
                json=batch,
                timeout=60,
                verify=verify_flag,
            )
            status_codes.extend([resp.status_code] * len(batch))
            if resp.status_code >= 400:
                self.logger.error(
                    "Batch POST /surveys/batch failed with status %s: %s",
                    resp.status_code,
                    resp.text,
                )
                resp.raise_for_status()

        self.logger.info("Synced %s survey rows via batch API", len(status_codes))
        return status_codes
