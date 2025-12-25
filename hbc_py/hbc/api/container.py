import logging
import hashlib
import json

import pandas as pd

from hbc import utils as ul
from hbc.ltp.loading import Fetcher, Validator
from hbc.ltp.persistence.rest import RestApi

logger = logging.getLogger()


class DataContainer:
    """Orchestrates loading, caching, and retrieving datasets by config name."""

    def __init__(self, config_name):
        """Load config by name and prepare empty DataFrame for results."""
        self.config = ul.get_config(config_name)
        self.moniker = self.config["moniker"]
        self.schema_cols = self._schema_columns(self.config)
        self._df: pd.DataFrame = pd.DataFrame(columns=self.schema_cols)

    def get(self, query: str | None = None):
        """Fetch fresh data using the configured fetcher and store in `df`."""
        fetcher_name: str = self.config["fetcher"]
        fetcher: Fetcher = Fetcher.from_name(fetcher_name)
        # Select validator via config, defaulting to generic.
        validator_name = self.config.get("validator", "ValidatorGeneric")
        validator: Validator = Validator.from_name(validator_name)
        df_raw = fetcher.fetch(self.config, query=query)
        df_validated = validator.parse(df_raw)
        df_with_key = self._add_hbc_unique_key(df_validated)
        self.df = df_with_key
        logger.info(f'Retrieved dataFrame with shape={self.df.shape}')

    @property
    def df(self) -> pd.DataFrame:
        """DataFrame backing the container; validated against config schema."""
        return self._df

    @df.setter
    def df(self, value: pd.DataFrame):
        """Set DataFrame and validate it against configured schema."""
        if not isinstance(value, pd.DataFrame):
            raise TypeError("df must be a pandas DataFrame")
        value = self._ensure_schema(value)
        self._df = value

    def to_cache(self):
        """Persist the current DataFrame via REST API."""
        RestApi().post(self.moniker, self.df, verify=False)

    def from_cache(self, query=None):
        """Load cached data via REST API."""
        api = RestApi()
        self.df = api.get(self.moniker, query if query else None)
        logger.info(f'Retrieved dataFrame with shape={self.df.shape}')
        
        

    def _ensure_schema(self, df: pd.DataFrame) -> pd.DataFrame:
        """
        Ensure df contains all schema columns; add missing columns as None.
        """
        schema_cols = set(self.schema_cols)
        missing_cols = sorted(schema_cols - set(df.columns))
        if missing_cols:
            logger.warning(
                "DataContainer %s missing columns; filling with None: %s",
                self.moniker,
                ", ".join(missing_cols),
            )
            for col in missing_cols:
                df[col] = None
        # reorder to schema order plus any extras at the end
        ordered_cols = list(self.schema_cols) + [
            c for c in df.columns if c not in self.schema_cols
        ]
        return df[ordered_cols]

    @staticmethod
    def _schema_columns(config: dict) -> list[str]:
        """Extract column names from schema supporting both list[str] and list[dict]."""
        cols = []
        for item in config.get("schema", []):
            if isinstance(item, dict):
                name = item.get("name") or item.get("column") or item.get("field")
                if name:
                    cols.append(name)
            else:
                cols.append(str(item))
        return cols

    @staticmethod
    def _add_hbc_unique_key(df: pd.DataFrame) -> pd.DataFrame:
        """Add deterministic hbc_unique_key column based on row contents."""
        if df is None or df.empty:
            return df
        key_col = "hbc_unique_key"
        if key_col in df.columns:
            return df

        def _hash_row(row):
            payload = {k: row[k] for k in df.columns if k != key_col}
            serialized = json.dumps(payload, sort_keys=True, default=str)
            return hashlib.sha1(serialized.encode("utf-8")).hexdigest()

        df = df.copy()
        df[key_col] = df.apply(_hash_row, axis=1)
        return df
