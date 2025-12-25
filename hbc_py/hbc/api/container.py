import datetime
import logging
import hashlib
import json

import pandas as pd

from hbc import utils as ul
from hbc.ltp.loading import Fetcher, Validator
from hbc.ltp.persistence.cache import Cache
from hbc.ltp.persistence.rest import RestApi
from hbc.ltp.persistence.db import SqlLiteDataBase

logger = logging.getLogger()


class DataContainer:
    """Orchestrates loading, caching, and retrieving datasets by config name."""

    def __init__(self, config_name):
        """Load config by name and prepare empty DataFrame for results."""
        self.config = ul.get_config(config_name)
        self.moniker = self.config["moniker"]
        self.schema_cols = self._schema_columns(self.config)
        self._df: pd.DataFrame = pd.DataFrame(columns=self.schema_cols)

    def get(self, **query_kwargs):
        """Fetch fresh data using the configured fetcher and store in `df`."""
        fetcher_name: str = self.config["fetcher"]
        fetcher: Fetcher = Fetcher.from_name(fetcher_name)
        if not query_kwargs:
            query_kwargs["limit"] = 100
        # Select validator via config, defaulting to generic.
        validator_name = self.config.get("validator", "ValidatorGeneric")
        validator: Validator = Validator.from_name(validator_name)
        df_raw = fetcher.fetch(self.config, **query_kwargs)
        df_validated = validator.parse(df_raw)
        df_with_key = self._add_hbc_unique_key(df_validated)
        self.df = df_with_key

    @property
    def df(self) -> pd.DataFrame:
        """DataFrame backing the container; validated against config schema."""
        return self._df

    @property
    def all_cached_dates(self):
        """List cached date folder names (sorted descending)."""
        return Cache.get_all_cached_dates(self)

    @df.setter
    def df(self, value: pd.DataFrame):
        """Set DataFrame and validate it against configured schema."""
        if not isinstance(value, pd.DataFrame):
            raise TypeError("df must be a pandas DataFrame")
        self._valid_schema(value)
        self._df = value

    def to_cache(self, as_of: datetime.date = None):
        """Persist the current DataFrame. Special-case service requests to SQLite."""
        if self.moniker == "nyc_open_data_311_customer_satisfaction_survey":
            RestApi().post(self.moniker, self.df, verify=False)
            return
        Cache.to_cache(self, as_of)

    def from_cache(
        self, as_of: datetime.date = None, retrieve_if_missing=False, query=None
    ):
        """Load cached data for the date; optionally fetch if cache is empty."""
        if self.moniker == "nyc_open_data_311_customer_satisfaction_survey":
            api = RestApi()
            query_str = query if query else None
            return api.get(self.moniker, query_str)
        self.df = Cache.from_cache(self, as_of)
        if not len(self.df) and retrieve_if_missing:
            self.get(as_of)
            self.to_cache(as_of)
        return self.df

    def _valid_schema(self, df: pd.DataFrame) -> bool:
        """Check that df contains all schema columns; log errors when missing."""
        schema_cols = set(self.schema_cols)
        missing_cols = sorted(schema_cols - set(df.columns))
        if missing_cols:
            logger.error(
                "DataContainer %s does not adhere to schema. Missing columns: %s",
                self.moniker,
                ", ".join(missing_cols),
            )
            return False
        return True

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
