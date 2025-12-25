import os
from pathlib import Path

import pandas as pd
import pandas.testing as pdt
import pytest
import requests
import hashlib
import json

from hbc import DataContainer, utils as ul
from hbc.ltp.persistence.rest import RestApi


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
    config = ul.get_config(moniker)
    # Build a single-row payload from schema with deterministic values.
    schema_items = config["schema"]
    schema_cols = [item["name"] if isinstance(item, dict) else str(item) for item in schema_items]

    def _dummy_value(item):
        if isinstance(item, dict):
            t = str(item.get("type", "")).lower()
        else:
            t = ""
        if t in {"datetime", "date_time"}:
            return "2024-01-01T00:00:00"
        if t in {"date"}:
            return "2024-01-01"
        if t in {"number", "int", "integer", "float"}:
            return 1
        return f"test_{item['name'] if isinstance(item, dict) else item}"

    payload = {col: _dummy_value(item) for col, item in zip(schema_cols, schema_items)}
    df = pd.DataFrame([payload])
    df = DataContainer._add_hbc_unique_key(df)

    dc = DataContainer(moniker)
    dc.df = df
    dc.to_cache()

    key = dc.df["hbc_unique_key"].iloc[0]
    dc.from_cache(query=f"$filter=hbc_unique_key eq '{key}'&$top=1")
    cached = dc.df.reset_index(drop=True)

    # Normalize strings (strip trailing .000 in timestamps) and fill nulls.
    def _normalize_df(df: pd.DataFrame) -> pd.DataFrame:
        df = df.copy()
        for c in df.columns:
            if c in {"start_time", "completion_time", "created_date", "closed_date", "date", "date_time"}:
                df[c] = pd.to_datetime(df[c], errors="coerce").dt.strftime("%Y-%m-%dT%H:%M:%S")
            else:
                df[c] = df[c].apply(
                    lambda v: str(v).rstrip(".000") if isinstance(v, str) else v
                )
        return df.fillna("")

    expected = _normalize_df(df.reset_index(drop=True))
    cached = _normalize_df(cached)

    # Align on shared columns
    common_cols = [c for c in expected.columns if c in cached.columns]
    expected = expected[common_cols]
    cached = cached[common_cols]

    pdt.assert_frame_equal(cached, expected, check_like=True, check_dtype=False)
