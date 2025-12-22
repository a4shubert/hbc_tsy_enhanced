import datetime
import logging

import pandas as pd

from hbc import utils as ul
from hbc.ltp.loading import Fetcher
from hbc.ltp.persistence.cache import Cache

logger = logging.getLogger()


class DataContainer:
    """Orchestrates loading, caching, and retrieving datasets by config name."""

    def __init__(self, config_name):
        """Load config by name and prepare empty DataFrame for results."""
        self.config = ul.get_config(config_name)
        self.moniker = self.config["moniker"]
        self._df: pd.DataFrame = pd.DataFrame(
            columns=self.config.get("schema", [])
        )
    

    def get(self, **query_kwargs):
        """Fetch fresh data using the configured fetcher and store in `df`."""
        fetcher_name: str = self.config["fetcher"]
        fetcher: Fetcher = Fetcher.from_name(fetcher_name)
        if not query_kwargs:
            query_kwargs["limit"] = 100        
        self.df = fetcher.get(self.config, **query_kwargs)

    @property
    def df(self) -> pd.DataFrame:
        """DataFrame backing the container; validated against config schema."""
        return self._df

    @df.setter
    def df(self, value: pd.DataFrame):
        """Set DataFrame and validate it against configured schema."""
        if not isinstance(value, pd.DataFrame):
            raise TypeError("df must be a pandas DataFrame")
        self._valid_schema(value)
        self._df = value



    def to_cache(self, as_of: datetime.date = None):
        """Persist the current DataFrame to the cache for the date."""
        Cache.to_cache(self, as_of)

    def from_cache(
        self, as_of: datetime.date = None, retrieve_if_missing=False
    ):
        """Load cached data for the date; optionally fetch if cache is empty."""
        self.df = Cache.from_cache(self, as_of)
        if not len(self.df) and retrieve_if_missing:
            self.get(as_of)
            self.to_cache(as_of)
        return self.df

    @property
    def all_cached_dates(self):
        """List cached date folder names (sorted descending)."""
        return Cache.get_all_cached_dates(self)

    def _valid_schema(self, df: pd.DataFrame) -> bool:
        """Check that df contains all schema columns; log errors when missing."""
        schema_cols = set(self.config.get("schema", []))
        missing_cols = sorted(schema_cols - set(df.columns))
        if missing_cols:
            logger.error(
                "DataContainer %s does not adhere to schema. Missing columns: %s",
                self.moniker,
                ", ".join(missing_cols),
            )
            return False
        return True
