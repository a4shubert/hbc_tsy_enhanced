import datetime as _dt
import logging
import time
from typing import Dict, Optional
from urllib.parse import parse_qsl

import pandas as pd
from sodapy import Socrata

from hbc import utils as ul
from hbc.ltp.loading.fetchers.base import Fetcher

logger = logging.getLogger()

CONST_PAGE_SIZE = 50_000  # per-page when paging


class FetcherNYCOpenData(Fetcher):
    """Fetcher implementation for NYC Open Data 311 service requests."""

    @classmethod
    def fetch(cls, config: Dict, query: Optional[str] = None) -> pd.DataFrame:
        """
        Fetch rows from Socrata API using an OData-like query string or legacy kwargs.
        Supported parameters (passed through to Socrata):
        select, where/$filter, order/$orderBy, group, limit/$top, offset, q, query, exclude_system_fields.
        """
        token = config["token"]
        base_url = config["base_url"]  # e.g., "data.cityofnewyork.us"
        dataset = config["url"]  # e.g., "3rfa-3xsf"
        timeout = int(config.get("timeout", 30))
        retries = int(config.get("retries", 3))
        page_size = int(config.get("page_size", 10_000))
        client = Socrata(base_url, app_token=token, timeout=timeout)

        query_params = cls._parse_query(query)

        def fetch_once():
            if query_params and "limit" in query_params:
                return client.get(dataset, **query_params)
            paged_kwargs = dict(query_params)
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

    @staticmethod
    def _parse_query(query: Optional[str]) -> Dict:
        """Convert query string into Socrata parameter dict."""
        params: Dict[str, object] = {}

        apply_seen = False

        if query:
            for k, v in parse_qsl(query, keep_blank_values=False):
                params[k] = v
                if k.lower() in {"$apply", "apply"}:
                    apply_seen = True

        # Map OData-ish keys to Socrata keys.
        key_map = {
            "$top": "limit",
            "top": "limit",
            "$filter": "where",
            "filter": "where",
            "$order": "order",
            "$orderby": "order",
            "orderby": "order",
            "$apply": "$apply",
        }
        mapped: Dict[str, object] = {}
        for k, v in params.items():
            target = key_map.get(k.lower(), k)
            mapped[target] = v

        # Convenience: if created_date/date provided without where, build where.
        if "where" not in mapped:
            date_val = mapped.pop("date", None) or mapped.pop("created_date", None)
            if date_val:
                dt = ul.str_as_date(str(date_val))
                mapped["where"] = f"created_date = '{ul.date_as_iso_format(dt)}'"
        else:
            # Normalize OData-ish operators to Socrata.
            mapped["where"] = ul.odata_filter_to_soql(str(mapped["where"]))

        # Handle simple $apply=groupby((col)) => select/group
        if "$apply" in mapped:
            apply_val = str(mapped.pop("$apply"))
            if apply_val.startswith("groupby((") and apply_val.endswith("))"):
                col = apply_val[len("groupby((") : -2]
                col = col.strip()
                if col:
                    mapped["select"] = col
                    mapped["group"] = col

        # Default limit if none specified and no filter/apply; if filter/apply present, no limit.
        has_filter = "where" in mapped and mapped["where"]
        has_apply = apply_seen or ("select" in mapped and "group" in mapped)
        if "limit" not in mapped and not has_filter and not has_apply:
            mapped["limit"] = 100

        return mapped
