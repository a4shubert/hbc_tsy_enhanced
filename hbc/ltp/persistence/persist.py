import datetime
import logging
import os.path
from pathlib import Path
from typing import TYPE_CHECKING

import pandas as pd

from hbc import app_context, utils as ul

if TYPE_CHECKING:
    from hbc.api.container import DataContainer

logger = logging.getLogger()


class Persistence:
    """Cache helper for reading/writing DataContainer CSV snapshots."""

    @classmethod
    def to_cache(cls, dc: "DataContainer", as_of: datetime):
        """Write the container DataFrame to cache for the given date."""
        if not as_of:
            as_of = app_context.as_of
        if not len(dc.df):
            logger.info(f"DataFrame is empty in {dc.moniker}")
            return
        path_cache = (
            ul.mk_dir(ul.get_dir_cache(dc.moniker) / ul.date_as_str(as_of))
            / f"{dc.moniker}.csv"
        )
        dc.df.to_csv(path_cache, index=False)
        logger.info(f"Cached: {path_cache}")

    @classmethod
    def from_cache(cls, dc: "DataContainer", as_of: datetime):
        """Load cached CSV for the date; return empty DataFrame if missing."""
        if not as_of:
            as_of = app_context.as_of
        path_cache = (
            ul.mk_dir(ul.get_dir_cache(dc.moniker) / ul.date_as_str(as_of))
            / f"{dc.moniker}.csv"
        )
        if os.path.exists(path_cache):
            logger.info(f"Retrieved from cache: {path_cache}")
            return pd.read_csv(path_cache, keep_default_na=False)
        else:
            logger.info(f"Path {path_cache} does not exist")
            return pd.DataFrame()

    @classmethod
    def get_all_cached_dates(cls, dc: "DataContainer") -> list[str]:
        """Return sorted list of cached date directory names (desc)."""
        path_cache: Path = ul.get_dir_cache(dc.moniker)  # ensures base exists
        return sorted(
            (p.name for p in path_cache.iterdir() if p.is_dir()), reverse=True
        )
