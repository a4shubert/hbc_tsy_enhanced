import pandas as pd
import pandas.testing as pdt
import pytest
from pathlib import Path

from hbc import utils as ul
from hbc.ltp.loading.fetchers.fetch_nycopen import FetcherNYCOpenData
from hbc.ltp.loading.fetchers import fetch_nycopen


BENCH_DIR = Path(__file__).resolve().parent / "benchmarks"


def _load_baseline(moniker: str) -> pd.DataFrame:
    path = BENCH_DIR / f"{moniker}.csv"
    assert path.exists(), f"Missing benchmark file: {path}"
    df = pd.read_csv(path, index_col=False)
    return df


@pytest.mark.parametrize(
    "moniker",
    [
        "nyc_open_data_311_service_requests",
        "nyc_open_data_311_call_center_inquiry",
        "nyc_open_data_311_customer_satisfaction_survey",
    ],
)
def test_fetcher_returns_expected_rows(monkeypatch, moniker):
    baseline = _load_baseline(moniker)
    config = ul.get_config(moniker)

    # Patch Socrata client to return baseline deterministically.
    class FakeClient:
        def __init__(self, *args, **kwargs):
            self.calls = []

        def get(self, dataset, **kwargs):
            self.calls.append(("get", dataset, kwargs))
            return baseline.to_dict(orient="records")

        def get_all(self, dataset, **kwargs):
            self.calls.append(("get_all", dataset, kwargs))
            return baseline.to_dict(orient="records")

    monkeypatch.setattr(fetch_nycopen, "Socrata", FakeClient)

    queries = {
        "nyc_open_data_311_service_requests": "$filter=unique_key eq '10000001'&$top=100",
        "nyc_open_data_311_call_center_inquiry": "$filter=unique_id eq '1001'&$top=100",
        "nyc_open_data_311_customer_satisfaction_survey": "$filter=campaign eq 'Campaign 1'&$top=100",
    }

    df = FetcherNYCOpenData.fetch(config, query=queries[moniker])

    expected = baseline.copy().reset_index(drop=True).fillna("")
    actual = df.reset_index(drop=True)
    actual = actual[expected.columns].fillna("")

    pdt.assert_frame_equal(actual, expected, check_like=True, check_dtype=False)
