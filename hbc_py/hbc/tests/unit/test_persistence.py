import pandas as pd
import pandas.testing as pdt
import pytest
from pathlib import Path

from hbc import DataContainer
from hbc.ltp.loading.fetchers import fetch_nycopen
from hbc.ltp.loading.validators.valid_nycopen import ValidatorNYCOpen311Service
from hbc.ltp.persistence.rest import RestApi

BENCH_DIR = Path(__file__).resolve().parent / "benchmarks"


def _load_baseline(moniker: str) -> pd.DataFrame:
    path = BENCH_DIR / f"{moniker}.csv"
    assert path.exists(), f"Missing benchmark file: {path}"
    return pd.read_csv(path, index_col=False)


@pytest.mark.parametrize(
    "moniker,query",
    [
        (
            "nyc_open_data_311_service_requests",
            "$filter=unique_key eq '10000001'&$top=100",
        ),
        (
            "nyc_open_data_311_call_center_inquiry",
            "$filter=unique_id eq '1001'&$top=100",
        ),
        (
            "nyc_open_data_311_customer_satisfaction_survey",
            "$filter=campaign eq 'Campaign 1'&$top=100",
        ),
    ],
)
def test_persistence_roundtrip(monkeypatch, moniker, query):
    baseline = _load_baseline(moniker)
    monkeypatch.setenv("HBC_API_URL", "http://test")

    # Patch Socrata client to return the baseline deterministically.
    class FakeClient:
        def __init__(self, *args, **kwargs):
            pass

        def get(self, dataset, **kwargs):
            return baseline.to_dict(orient="records")

        def get_all(self, dataset, **kwargs):
            return baseline.to_dict(orient="records")

    monkeypatch.setattr(fetch_nycopen, "Socrata", FakeClient)

    # Avoid dropping rows for service requests so baseline rows remain.
    monkeypatch.setattr(
        ValidatorNYCOpen311Service, "drop_flagged", staticmethod(lambda df: df)
    )

    # In-memory REST store.
    store: dict[str, pd.DataFrame] = {}

    def fake_post(self, table: str, df: pd.DataFrame, verify=None):
        store[table] = df.copy()
        return [200] * len(df)

    def fake_get(self, table: str, query: str | None = None, verify=None):
        return store.get(table, pd.DataFrame()).copy()

    monkeypatch.setattr(RestApi, "post", fake_post)
    monkeypatch.setattr(RestApi, "get", fake_get)

    dc = DataContainer(moniker)
    dc.get(query=query)
    expected = dc.df.copy().reset_index(drop=True)

    dc.to_cache()
    dc.from_cache(query=query)

    actual = dc.df.reset_index(drop=True)

    # Compare only columns present in expected (ignore hbc_unique_key if added later)
    actual = actual[expected.columns]

    expected = expected.fillna("")
    actual = actual.fillna("")

    pdt.assert_frame_equal(actual, expected, check_like=True, check_dtype=False)
