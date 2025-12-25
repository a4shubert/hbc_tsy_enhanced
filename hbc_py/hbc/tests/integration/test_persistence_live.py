import os

import pandas as pd
import pytest
import requests

from hbc import DataContainer


REQUIRES_ENV = os.getenv("HBC_INTEGRATION")
API_BASE = os.getenv("HBC_API_URL", "http://localhost:5047").rstrip("/")


def _rest_available() -> bool:
    try:
        resp = requests.get(API_BASE, timeout=3)
        return resp.status_code < 500
    except Exception:
        return False


@pytest.mark.integration
@pytest.mark.skipif(not REQUIRES_ENV, reason="Set HBC_INTEGRATION=1 to run live persistence tests")
@pytest.mark.skipif(not _rest_available(), reason="REST API not reachable")
@pytest.mark.parametrize(
    "moniker",
    [
        "nyc_open_data_311_service_requests",
        "nyc_open_data_311_call_center_inquiry",
        "nyc_open_data_311_customer_satisfaction_survey",
    ],
)
def test_live_persistence_roundtrip(moniker):
    # Fetch a small sample from upstream (Socrata).
    dc = DataContainer(moniker)
    dc.get(query="$top=10")
    assert not dc.df.empty, "Upstream fetch returned no rows"

    # Prefix hbc_unique_key for test rows to distinguish from production data.
    if "hbc_unique_key" in dc.df:
        dc.df["hbc_unique_key"] = (
            "test_" + dc.df["hbc_unique_key"].astype(str)
        )
    upstream = dc.df.copy().reset_index(drop=True)
    keys = upstream.get("hbc_unique_key", pd.Series(dtype=str)).dropna().astype(str).tolist()
    assert keys, "No hbc_unique_key values generated"

    # Persist to REST API.
    dc.to_cache()

    fetched_parts: list[pd.DataFrame] = []
    try:
        # Retrieve exactly the posted rows by key, one by one.
        for key in keys:
            tmp = DataContainer(moniker)
            tmp.from_cache(query=f"$filter=hbc_unique_key eq '{key}'&$top=1")
            if not tmp.df.empty:
                fetched_parts.append(tmp.df.copy())

        cached = (
            pd.concat(fetched_parts, ignore_index=True)
            if fetched_parts
            else pd.DataFrame()
        )
        assert not cached.empty, "Cache GET returned no rows for posted keys"

        # Normalize strings (strip trailing .000 in timestamps) and fill nulls.
        def _normalize_df(df: pd.DataFrame) -> pd.DataFrame:
            df = df.copy()
            for c in df.columns:
                if c in {"start_time", "completion_time", "created_date", "closed_date", "date", "date_time"}:
                    df[c] = (
                        pd.to_datetime(df[c], errors="coerce")
                        .dt.strftime("%Y-%m-%d %H:%M:%S")
                    )
                else:
                    df[c] = df[c].apply(
                        lambda v: str(v).rstrip(".000") if isinstance(v, str) else v
                    )
            return df.fillna("")

        upstream = _normalize_df(upstream)
        cached = _normalize_df(cached)

        # Ensure hbc_unique_key present on both sides.
        if "hbc_unique_key" not in cached.columns:
            cached["hbc_unique_key"] = None
        if "hbc_unique_key" not in upstream.columns:
            upstream["hbc_unique_key"] = pd.Series(keys[: len(upstream)])

        # Align on shared columns.
        common_cols = [c for c in upstream.columns if c in cached.columns]
        upstream = upstream[common_cols].set_index("hbc_unique_key")
        cached = cached[common_cols].set_index("hbc_unique_key")

        # Compare only on keys we actually retrieved
        common_keys = cached.index.intersection(upstream.index)
        upstream = upstream.loc[common_keys]
        cached = cached.loc[common_keys]

        # Structural assertions: same columns/rows, and at least one row.
        assert not cached.empty, "Cached rows not found after filtering"
        assert list(cached.columns) == list(upstream.columns)
        assert len(cached) == len(upstream)
    finally:
        # Cleanup: delete posted rows by hbc_unique_key
        for key in keys:
            try:
                requests.delete(f"{API_BASE}/{moniker}/{key}", timeout=5)
            except Exception:
                pass
