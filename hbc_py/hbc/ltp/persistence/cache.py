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


class Cache:
    """Cache helper for reading/writing DataContainer CSV snapshots."""

    @classmethod
    def to_cache(cls, dc: "DataContainer", as_of: datetime):
        """Write the container DataFrame to cache for the given date."""
        if not as_of:
            as_of = app_context.as_of
        if not len(dc.df):
            logger.info(f"DataFrame is empty in {dc.moniker}")
            return
        cache_dir = ul.mk_dir(
            app_context.dir_cache / dc.moniker / ul.date_as_str(as_of)
        )
        csv_path = cache_dir / f"{dc.moniker}.csv"
        dc.df.to_csv(csv_path, index=False)
        gz_path = ul.gz_file(csv_path)
        logger.info(f"Cached: {gz_path}")

    @classmethod
    def from_cache(cls, dc: "DataContainer", as_of: datetime):
        """Load cached CSV for the date; return empty DataFrame if missing."""
        if not as_of:
            as_of = app_context.as_of
        cache_dir = ul.mk_dir(
            app_context.dir_cache / dc.moniker / ul.date_as_str(as_of)
        )
        csv_path = cache_dir / f"{dc.moniker}.csv"
        gz_path = csv_path.with_suffix(csv_path.suffix + ".gz")

        if os.path.exists(gz_path):
            temp_csv = ul.un_gz_file(gz_path)
            try:
                df = pd.read_csv(temp_csv, keep_default_na=False)
            finally:
                ul.gz_file(temp_csv)
            logger.info(f"Retrieved from cache: {gz_path}")
            return df
        if os.path.exists(csv_path):
            logger.info(f"Retrieved from cache (plain): {csv_path}")
            df = pd.read_csv(csv_path, keep_default_na=False)
            gz_path = ul.gz_file(csv_path)
            logger.info(f"Compressed cache file: {gz_path}")
            return df

        logger.info(f"Path {gz_path} does not exist")
        return pd.DataFrame()

    @classmethod
    def get_all_cached_dates(cls, dc: "DataContainer") -> list[str]:
        """Return sorted list of cached date directory names (desc)."""
        path_cache: Path = ul.mk_dir(app_context.dir_cache / dc.moniker)
        return sorted(
            (p.name for p in path_cache.iterdir() if p.is_dir()), reverse=True
        )
