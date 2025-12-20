import datetime
import os.path
from pathlib import Path
from typing import TYPE_CHECKING

import pandas as pd

from hbc import utils as ul, app_context

if TYPE_CHECKING:
    from hbc.abstract.container import DataContainer


class Persistence:
    @classmethod
    def to_cache(cls, dc: "DataContainer", as_of: datetime):
        if not as_of:
            as_of = app_context.as_of
        if not len(dc.df):
            print(f"DataFrame is empty in {dc.moniker}")
            return
        path_cache = (
            ul.mk_dir(ul.get_cache_dir(dc.moniker) / ul.date_as_str(as_of))
            / f"{dc.moniker}.csv"
        )
        dc.df.to_csv(path_cache, index=False)
        print("ok")

    @classmethod
    def from_cache(cls, dc: "DataContainer", as_of: datetime):
        if not as_of:
            as_of = app_context.as_of
        path_cache = (
            ul.mk_dir(ul.get_cache_dir(dc.moniker) / ul.date_as_str(as_of))
            / f"{dc.moniker}.csv"
        )
        if os.path.exists(path_cache):
            return pd.read_csv(path_cache)
        else:
            return pd.DataFrame()

    @classmethod
    def get_all_cache_dates(cls, dc: "DataContainer") -> list[str]:
        path_cache: Path = ul.get_cache_dir(dc.moniker)  # ensures base exists
        return sorted(p.name for p in path_cache.iterdir() if p.is_dir())
