import datetime

import pandas as pd

from hbc import utils as ul
from hbc.ltp.fetching import Fetcher
from hbc.ltp.fetching import fetchers_registry
from hbc.ltp.persistence.persist import Persistence


class DataContainer:
    def __init__(self, config_name):
        self.config = ul.get_config(config_name)
        self.moniker = self.config["moniker"]
        self.df: pd.DataFrame = pd.DataFrame()

    def get(self):
        fetcher: Fetcher = fetchers_registry[self.moniker]
        self.df = fetcher.get(self.config)

    def to_cache(self, as_of: datetime.date = None):
        Persistence.to_cache(self, as_of)

    def from_cache(self, as_of: datetime.date = None):
        self.df = Persistence.from_cache(self, as_of)
        return self.df

    @property
    def all_cached_dates(self):
        return Persistence.get_all_cache_dates(self)
