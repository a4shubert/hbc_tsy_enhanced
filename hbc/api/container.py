import datetime
import logging

import pandas as pd

from hbc import app_context, utils as ul
from hbc.ltp.loading import Fetcher
from hbc.ltp.persistence.persist import Persistence

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

    @property
    def df(self) -> pd.DataFrame:
        """DataFrame backing the container; validated against config schema."""
        return self._df

    @df.setter
    def df(self, value: pd.DataFrame):
        """Set DataFrame and validate it against configured schema."""
        if not isinstance(value, pd.DataFrame):
            raise TypeError("df must be a pandas DataFrame")
        if self._valid_schema(value):
            self._df = value

    def get(self, as_of: datetime.date | str = None):
        """Fetch fresh data for the given as-of date and store in `df`."""
        as_of_date = ul.str_as_date(as_of) or app_context.as_of
        fetcher_name: str = self.config["fetcher"]
        fetcher: Fetcher = Fetcher.from_name(fetcher_name)
        self.df = fetcher.get(self.config, as_of_date)

    def to_cache(self, as_of: datetime.date = None):
        """Persist the current DataFrame to the cache for the date."""
        Persistence.to_cache(self, as_of)

    def from_cache(
        self, as_of: datetime.date = None, retrieve_if_missing=False
    ):
        """Load cached data for the date; optionally fetch if cache is empty."""
        self.df = Persistence.from_cache(self, as_of)
        if not len(self.df) and retrieve_if_missing:
            self.get(as_of)
            self.to_cache(as_of)
        return self.df

    @property
    def all_cached_dates(self):
        """List cached date folder names (sorted descending)."""
        return Persistence.get_all_cached_dates(self)

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
