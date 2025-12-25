import logging

import pandas as pd

from hbc import app_context, DataContainer, utils as ul


logger = logging.getLogger()

LIMIT_MISS_DATES = 10


def job_fetch_nyc_open_data_311_service_requests(
    as_of: str = None,
    incremental=True,
    last_missing_dates=LIMIT_MISS_DATES,
):
    """
    Job for polling nyc_open_data
    :param last_missing_dates:
    :param as_of:
    :param incremental: if True we only poll the missing data, otherwise we retrieve entire dataset
    one created_date at a time
    :return:
    """
    logger.info(f"\n\nRunning job_fetch_nyc_open_data_311_service_requests\n\n")

    if not as_of:
        as_of = app_context.as_of

    dc = DataContainer("nyc_open_data_311_service_requests")

    if incremental:
        logger.info(
            "Running job_fetch_nyc_open_data_311_service_requests for %s (incremental)",
            as_of,
        )
        date_str = ul.date_as_iso_format(ul.str_as_date(as_of))
        dc.get(query=f"$filter=created_date eq '{date_str}'")
        dc.to_cache()
        return

    # Non-incremental: pull distinct created_date values from source and fetch each.
    logger.info(
        "Running job_fetch_nyc_open_data_311_service_requests full sync (last %s dates)",
        last_missing_dates,
    )
    dc.get(query="$apply=groupby((created_date))")
    dates = (
        pd.to_datetime(dc.df["created_date"], errors="coerce")
        .dropna()
        .sort_values(ascending=False)
        .head(last_missing_dates)
    )
    for d in dates:
        date_str = ul.date_as_iso_format(d.date())
        logger.info("Fetching created_date=%s", date_str)
        dc_day = DataContainer("nyc_open_data_311_service_requests")
        dc_day.get(query=f"$filter=created_date eq '{date_str}'")
        dc_day.to_cache()
