import datetime as _dt
import logging
import time
from typing import Dict

import pandas as pd
from sodapy import Socrata

from hbc import utils as ul
from hbc.ltp.loading.fetchers.base import Fetcher

logger = logging.getLogger()

CONST_PAGE_SIZE = 50_000  # per-page when paging


class FetcherNYCOpenData(Fetcher):
    """Fetcher implementation for NYC Open Data 311 service requests."""

    @property
    def validator_name(self) -> str:
        return "ValidatorNYCOpen311Service"

    @classmethod
    def fetch(cls, config: Dict, **query_kwargs) -> pd.DataFrame:
        """
        Fetch rows from Socrata API using provided query kwargs.
        Supported Socrata parameters (passed through unchanged):
        select, where, order, group, limit, offset, q, query, exclude_system_fields.
        Convenience: pass `date=` or `created_date=` (YYYY-MM-DD or yyyymmdd)
        to auto-build a where clause on created_date if none is provided.
        """
        token = config["token"]
        base_url = config["base_url"]  # e.g., "data.cityofnewyork.us"
        dataset = config["url"]  # e.g., "3rfa-3xsf"
        timeout = int(config.get("timeout", 30))
        retries = int(config.get("retries", 3))
        page_size = int(config.get("page_size", 10_000))
        client = Socrata(base_url, app_token=token, timeout=timeout)

        # convenience: allow date/created_date to define where clause
        if "where" not in query_kwargs:
            date_val = query_kwargs.pop(
                "date", query_kwargs.pop("created_date", None)
            )
            if date_val is not None:
                dt = ul.str_as_date(date_val)
                query_kwargs["where"] = (
                    f"created_date = '{ul.date_as_iso_format(dt)}'"
                )
        else:
            query_kwargs.pop("date", None)
            query_kwargs.pop("created_date", None)

        def fetch_once():
            if query_kwargs and "limit" in query_kwargs:
                return client.get(dataset, **query_kwargs)
            paged_kwargs = dict(query_kwargs)
            paged_kwargs["limit"] = page_size
            logger.info(
                "using pagination at fetching with page_size=%s timeout=%s",
                page_size,
                timeout,
            )
            return list(client.get_all(dataset, **paged_kwargs))

        last_exc = None
        for attempt in range(retries):
            try:
                rows = fetch_once()
                break
            except Exception as exc:
                last_exc = exc
                if attempt == retries - 1:
                    raise
                sleep_for = 2**attempt
                logger.warning(
                    "Fetch attempt %s/%s failed (%s); retrying in %ss",
                    attempt + 1,
                    retries,
                    exc,
                    sleep_for,
                )
                time.sleep(sleep_for)
        else:
            raise last_exc

        df = pd.DataFrame.from_records(rows)
        logger.info(f"Fetched {len(df)} rows")
        return df
