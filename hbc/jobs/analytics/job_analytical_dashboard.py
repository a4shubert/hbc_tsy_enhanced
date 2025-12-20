import pandas as pd

from hbc import DataContainer, app_context, utils as ul
from hbc.quant.analysis import AnalyticalEngine
from hbc.quant.plots import PlotEngine


def job_nyc_open_data_analysis(
        as_of: str = None,
        n_worst=10,
        n_best=10,
        n_days=10,
):
    if not as_of:
        as_of = app_context.as_of

    print(
        f"\n\nRunning job_nyc_open_data_analysis as_of={as_of} n_worst={n_worst} n_best={n_best}  \n\n"
    )

    dc = DataContainer("nyc_open_data_311_service_requests")
    dc.from_cache(as_of, retrieve_if_missing=True)

    df = dc.df.copy()
    cols = ul.cols_as_named_tuple(df)

    dir_analytics = ul.mk_dir(
        app_context.dir_analytics
        / dc.moniker
        / ul.date_as_str(app_context.as_of)
    )
    # for the moment we'll drop all the DROP_FLAG = TRUE rows
    df_drop = df[df[cols.DROP_FLAG]].copy()
    df_drop.to_csv(ul.mk_dir(dir_analytics / 'tables') / "df_dropped.csv", index=False)
    df = df[~df[cols.DROP_FLAG]]

    df["hbc_days_to_close"] = (
            pd.to_datetime(df[cols.closed_date])
            - pd.to_datetime(df[cols.created_date])
    ).dt.days.astype("Int64")
    cols = ul.cols_as_named_tuple(df)

    # we will partition dataset into 3 parts
    # - closed same day
    # - closed after 1 or more days
    # - never closed
    m = df[cols.hbc_days_to_close] == 0
    df_closed_same_day = df[m]
    df_closed_not_same_day = df[~m]

    m = df[cols.hbc_days_to_close].isna()
    df_open = df[m]

    # we will just persist df_closed_same_day as there is no descriptive statistics needed (all 0)
    df_closed_same_day.to_csv(
        ul.mk_dir(dir_analytics / 'tables') / "df_closed_same_day.csv", index=False
    )

    # we want to get descriptive statistics for the df_closed_not_same_day
    if len(df_closed_not_same_day):
        # we want overall geomap for all the closed requests by location
        PlotEngine.plot_geo_map(
            df=df_closed_not_same_day,
            col_latitude=cols.latitude,
            col_longitude=cols.longitude,
            aggregation="count",
            round_precision=3,
            cluster=True,
            start_zoom=11,
            tiles="CartoDB positron",
            savepath=ul.path_to_str(ul.mk_dir(dir_analytics / 'plots') / "closed_requests_by_location.html"),
        )

        # by agency
        res = AnalyticalEngine.descriptive_stats(
            n_best=n_best,
            n_worst=n_worst,
            df=df_closed_not_same_day,
            col_metric=cols.hbc_days_to_close,
            group=[
                cols.agency,
                cols.agency_name,
            ],
        )

        PlotEngine.plot_bar(
            df=df_closed_not_same_day,
            group_cols=[
                cols.agency,
                cols.agency_name,
            ],
            aggregation="count",
            top_n=15,
            orient="h",
            percent=True,
            ascending=False,
            largest_on_top=True,
            percent_base="plotted",
            title=f"Top Worst Agencies by Requests (%)",
            show=False,
            savepath=ul.path_to_str(ul.mk_dir(dir_analytics / 'plots') / "top_worst_agencies_closed_distro.png"),
        )

        xlsx_name = "closed_by_agency.xlsx"
        for name, df_analytics in res.items():
            ul.save_dataframe_as_sheet(
                ul.mk_dir(dir_analytics / 'tables'),
                xlsx_name,
                df_analytics.round(2),
                name,
                replace=True,
            )

        # by agency / complaint_type
        res = AnalyticalEngine.descriptive_stats(
            n_best=n_best,
            n_worst=n_worst,
            df=df_closed_not_same_day,
            col_metric=cols.hbc_days_to_close,
            group=[cols.agency, cols.agency_name, cols.complaint_type],
        )

        xlsx_name = "closed_by_agency_complaint_type.xlsx"
        for name, df_analytics in res.items():
            ul.save_dataframe_as_sheet(
                ul.mk_dir(dir_analytics / 'tables'),
                xlsx_name,
                df_analytics.round(2),
                name,
                replace=True,
            )

        # by city / by agency
        res = AnalyticalEngine.descriptive_stats(
            n_best=n_best,
            n_worst=n_worst,
            df=df_closed_not_same_day,
            col_metric=cols.hbc_days_to_close,
            group=[cols.city, cols.agency, cols.agency_name],
        )

        xlsx_name = "closed_by_city_agency.xlsx"
        for name, df_analytics in res.items():
            ul.save_dataframe_as_sheet(
                ul.mk_dir(dir_analytics / 'tables'),
                xlsx_name,
                df_analytics.round(2),
                name,
                replace=True,
            )

    if len(df_open):
        # by agency / by city / by complaint_type
        group = [
            cols.agency,
            cols.agency_name,
            cols.city,
            cols.complaint_type,
        ]
        df_analytics = (
            df_open.groupby(group)[cols.unique_key]
            .count()
            .sort_index()
            .sort_values(ascending=False)
            .reset_index()
            .rename(columns={cols.unique_key: "count_requests"})
        )

        xlsx_name = "open_by_agency_city_complaint_type.xlsx"
        ul.save_dataframe_as_sheet(
            ul.mk_dir(dir_analytics / 'tables'),
            xlsx_name,
            df_analytics.round(2),
            "grouped",
            replace=True,
        )

    # the last one will be the time series  / tren analysis of the worst agency over the last n_days
    df = pd.concat([dc.from_cache(t) for t in dc.all_cached_dates[:n_days]])

    worst_agency = df[cols.agency_name].value_counts().idxmax()
    _ = PlotEngine.plot_ts(
        df=df,
        col_time=cols.created_date,
        col_metric=None,
        add_trend=True,
        trend_window=7,
        freq="D",
        aggregation="count",
        filter_by={cols.agency_name: worst_agency},
        title=f"Daily Requests for {worst_agency}",
        show=False,
        savepath=ul.path_to_str(ul.mk_dir(dir_analytics / 'plots') / "worst_agency_ts_analysis.png"),
    )

    print(f"Saved to {dir_analytics}")


