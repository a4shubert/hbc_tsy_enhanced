import pandas as pd

from hbc import utils as ul
from hbc.ltp.fetchers import Fetcher
from hbc.ltp.fetchers import fetchers_registry
from hbc.ltp.persist import Persistence


class DataContainer:
    def __init__(self, config_name):
        self.config = ul.get_config(config_name)
        self.moniker = self.config['moniker']
        self.df: pd.DataFrame = pd.DataFrame()

    def get(self):
        fetcher: Fetcher = fetchers_registry[self.moniker]
        self.df = fetcher.get(self.config)
        return self.df

    def to_cache(self):
        Persistence.to_cache(self)

    def from_cache(self):
        self.df = Persistence.from_cache(self)
        return self.df
