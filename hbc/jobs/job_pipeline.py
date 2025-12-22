from datetime import datetime
import logging

import pandas as pd

from hbc import app_context, DataContainer, utils as ul


logger = logging.getLogger()

LIMIT_MISS_DATES = 10


def job_poll_nyc_311(
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
    logger.info(f"\n\nRunning job_poll_nyc_open_data\n\n")

    if not as_of:
        as_of = app_context.as_of

    if incremental:
        logger.info(
            f"Running job_poll_nyc_open_data for {as_of} and incremental={incremental}"
        )
        dc = DataContainer("nyc_open_data_311_service_requests")
        dc.get(
            where=f"created_date = '{ul.date_as_iso_format(ul.str_as_date(as_of))}' "
        )
        dc.to_cache(as_of)
    else:
        # we are going to identify all the created_date(s) in the database that are missing in cache
        dc = DataContainer("nyc_open_data_311_service_requests")
        dc.get(select="created_date", group="created_date")
        all_dates = pd.to_datetime(dc.df["created_date"])
        cached_dates = pd.to_datetime(dc.all_cached_dates)
        missing_dates = set(all_dates).difference(cached_dates)
        if missing_dates:
            logger.info(
                f"Running job_poll_nyc_open_data for the last {last_missing_dates} dates:"
            )
            for as_of in sorted(list(missing_dates), reverse=True)[
                :last_missing_dates
            ]:
                logger.info(f"working {as_of}")
                dc = DataContainer("nyc_open_data_311_service_requests")
                dc.get(
                    where=(
                        f"created_date = '{ul.date_as_iso_format(as_of.date())}' "
                    )
                )
                dc.to_cache(as_of)
