import pandas as pd

from hbc import DataContainer, app_context, utils as ul
from hbc.quant.analysis import AnalyticalEngine


def job_nyc_open_data_analyse(
    as_of: str = None,
    incremental=True,
    n_worst=10,
    n_best=10,
):
    if not as_of:
        as_of = app_context.as_of

    print(
        f"\n\nRunning job_nyc_open_data_analyse as_of={as_of} n_worst={n_worst} n_best={n_best}  \n\n"
    )
    if incremental:
        dc = DataContainer("nyc_open_data_311_service_requests")
        dc.from_cache(as_of, retrieve_if_missing=True)

        df = dc.df.copy()

        dir_analytics = ul.mk_dir(
            app_context.dir_analytics
            / dc.moniker
            / ul.date_as_str(app_context.as_of)
        )
        # for the moment we'll drop all the DROP_FLAG = TRUE rows
        df_drop = df[df["DROP_FLAG"]].copy()
        df_drop.to_csv(dir_analytics / "df_dropped.csv", index=False)
        df = df[~df["DROP_FLAG"]]

        cols = ul.cols_as_named_tuple(df)
        df["hbc_days_to_close"] = (
            pd.to_datetime(df[cols.closed_date])
            - pd.to_datetime(df[cols.created_date])
        ).dt.days.astype("Int64")
        cols = ul.cols_as_named_tuple(df)

        # by agency
        res = AnalyticalEngine.descriptive_stats(
            n_best=n_best,
            n_worst=n_worst,
            df=df,
            metric_col=cols.hbc_days_to_close,
            group=[
                cols.agency,
                cols.agency_name,
            ],
        )

        xlsx_name = "by_agency.xlsx"
        for name, df_analytics in res.items():
            print(f"saving {name}")
            ul.save_dataframe_as_sheet(
                dir_analytics,
                xlsx_name,
                df_analytics.round(2),
                name,
                replace=True,
            )
        print(f"Saved to {dir_analytics}")

        # by agency / complaint_type
        res = AnalyticalEngine.descriptive_stats(
            n_best=n_best,
            n_worst=n_worst,
            df=df,
            metric_col=cols.hbc_days_to_close,
            group=[cols.agency, cols.agency_name, cols.complaint_type],
        )

        xlsx_name = "by_agency_complaint_type.xlsx"
        for name, df_analytics in res.items():
            print(f"saving {name}")
            ul.save_dataframe_as_sheet(
                dir_analytics,
                xlsx_name,
                df_analytics.round(2),
                name,
                replace=True,
            )
        print(f"Saved to {dir_analytics}")
    else:
        raise NotImplementedError(
            "we need to think how to implement analysis on the large dataset"
        )
