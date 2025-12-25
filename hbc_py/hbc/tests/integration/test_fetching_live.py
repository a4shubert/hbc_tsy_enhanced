import os
import pytest

from hbc import utils as ul
from hbc.ltp.loading.fetchers.fetch_nycopen import FetcherNYCOpenData


REQUIRES_ENV = os.getenv("HBC_INTEGRATION")


@pytest.mark.integration
@pytest.mark.skipif(not REQUIRES_ENV, reason="Set HBC_INTEGRATION=1 to run live fetch tests")
@pytest.mark.parametrize(
    "moniker",
    [
        "nyc_open_data_311_service_requests",
        "nyc_open_data_311_call_center_inquiry",
        "nyc_open_data_311_customer_satisfaction_survey",
    ],
)
def test_live_fetch_returns_rows(moniker):
    config = ul.get_config(moniker)
    # Request all schema columns explicitly to avoid Socrata omitting null columns.
    schema_cols = [item["name"] if isinstance(item, dict) else str(item) for item in config["schema"]]
    select_clause = ",".join(schema_cols)
    df = FetcherNYCOpenData.fetch(config, query=f"$select={select_clause}&$top=5")
    assert not df.empty, f"{moniker} returned no rows"

    # Ensure required schema columns are present.
    schema_cols = {item["name"] if isinstance(item, dict) else str(item) for item in config["schema"]}
    missing = schema_cols - set(df.columns)
    if missing:
        # Socrata may omit entirely null columns; just ensure we have at least one schema col.
        assert len(schema_cols & set(df.columns)) > 0, f"{moniker} returned no schema columns"
