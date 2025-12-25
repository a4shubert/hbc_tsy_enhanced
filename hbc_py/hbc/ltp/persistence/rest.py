import hashlib
import json
import logging
import os
from typing import Optional, List

import pandas as pd
import pandas.api.types as ptypes
import requests

logger = logging.getLogger()
MIN_ROWS_COUNT = 10

class RestApi:
    """Helper for interacting with the REST API."""

    def __init__(self):
        self.logger = logging.getLogger()
        self.api_base = os.environ.get("HBC_API_URL", "http://localhost:5047").rstrip("/")

    def get(
        self,
        table: str,
        query: Optional[str] = None,
        verify: Optional[bool] = None,
    ) -> pd.DataFrame:
        """
        Execute an OData-style GET against the API and return a DataFrame.

        `query` should be an OData query string (e.g., "$top=10", "$filter=col eq 'val'").
        """
        verify_flag = verify
        if verify_flag is None:
            env_verify = os.environ.get("HBC_API_VERIFY", "").strip().lower()
            if env_verify in {"false", "0", "no", "off"}:
                verify_flag = False
            elif env_verify in {"true", "1", "yes", "on"}:
                verify_flag = True

        url = f"{self.api_base}/{table}"
        if not query:            
            effective_query = f"$top={MIN_ROWS_COUNT}"
            logger.info(f' Without query retrieving only {MIN_ROWS_COUNT} rows')
        else:
            effective_query = query 
        url = f"{url}?{effective_query}"

        resp = requests.get(url, timeout=60, verify=verify_flag)
        if resp.status_code >= 400:
            self.logger.error(
                "GET %s failed with status %s: %s", url, resp.status_code, resp.text
            )
            resp.raise_for_status()
        data = resp.json()
        if isinstance(data, dict) and "value" in data:
            data = data["value"]
        return pd.DataFrame(data)

    def post(
        self,
        table: str,
        df: pd.DataFrame,
        verify: Optional[bool] = None,
    ) -> List[int]:
        """
        Push DataFrame rows to the REST API via batch endpoint named after the table.

        - Adds a deterministic hbc_unique_key hash if missing.
        - Deduplicates within the batch by hbc_unique_key.
        """
        if df is None or df.empty:
            self.logger.warning("DataFrame is empty; nothing to sync.")
            return []
        try:
            import requests
        except ImportError as exc:
            raise ImportError("requests package is required for API sync") from exc

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

        # Add deterministic hbc_unique_key hash column to help dedupe/upsert decisions.
        key_col = "hbc_unique_key"
        if key_col not in data.columns:
            def _hash_row(row):
                payload = {k: row[k] for k in data.columns if k != key_col}
                serialized = json.dumps(payload, sort_keys=True, default=str)
                return hashlib.sha1(serialized.encode("utf-8")).hexdigest()

            data[key_col] = data.apply(_hash_row, axis=1)

        # Drop duplicates on key within this payload.
        data = data.drop_duplicates(subset=[key_col])

        records = data.to_dict(orient="records")
        if not records:
            return []

        status_codes: list[int] = []
        chunk_size = 100
        for i in range(0, len(records), chunk_size):
            batch = records[i : i + chunk_size]
            self.logger.info(
                "Posting batch %s-%s/%s to %s/%s/batch (verify=%s)",
                i + 1,
                i + len(batch),
                len(records),
                self.api_base,
                table,
                verify_flag,
            )
            resp = requests.post(
                f"{self.api_base}/{table}/batch",
                json=batch,
                timeout=60,
                verify=verify_flag,
            )
            status_codes.extend([resp.status_code] * len(batch))
            if resp.status_code >= 400:
                self.logger.error(
                    "Batch POST %s failed with status %s: %s",
                    f"/{table}/batch",
                    resp.status_code,
                    resp.text,
                )
                resp.raise_for_status()

        self.logger.info("Synced %s rows via batch API", len(status_codes))
        return status_codes
