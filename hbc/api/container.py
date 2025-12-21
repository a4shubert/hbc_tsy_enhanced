import datetime

import pandas as pd

from hbc import utils as ul
from hbc.ltp.fetching import Fetcher
from hbc.ltp.persistence.persist import Persistence


class DataContainer:
    """Orchestrates fetching, caching, and retrieving datasets by config name."""

    def __init__(self, config_name):
        """Load config by name and prepare empty DataFrame for results."""
        self.config = ul.get_config(config_name)
        self.moniker = self.config["moniker"]
        self.df: pd.DataFrame = pd.DataFrame()

    def get(self, as_of: datetime.date | str = None):
        """Fetch fresh data for the given as-of date and store in `df`."""
        fetcher_name: str = self.config["fetcher"]
        fetcher: Fetcher = Fetcher.from_name(fetcher_name)
        self.df = fetcher.get(self.config, ul.str_as_date(as_of))

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
