from typing import TYPE_CHECKING

import pandas as pd

from hbc import utils as ul

if TYPE_CHECKING:
    from hbc.abstract.container import DataContainer


class Persistence:
    @classmethod
    def to_cache(cls, dc: 'DataContainer'):
        path_cache = ul.get_cache_dir(dc.moniker) / f"{dc.moniker}.csv"
        dc.df.to_csv(path_cache, index=False)
        print('ok')

    @classmethod
    def from_cache(cls, dc: 'DataContainer'):
        path_cache = ul.get_cache_dir(dc.moniker) / f"{dc.moniker}.csv"
        print('ok')
        return pd.read_csv(path_cache)
