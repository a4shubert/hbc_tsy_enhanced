import datetime as _dt
import logging
import re
from typing import Dict

import pandas as pd
from sodapy import Socrata

from hbc import app_context, utils as ul
from hbc.ltp.fetching.base import Fetcher
from hbc.utils import _parse_dt, _nz, _to_hashable_df


logger = logging.getLogger()

NYC_LAT_BOUNDS = (40.0, 41.0)
NYC_LON_BOUNDS = (-75.0, -72.0)
NYC_X_BOUNDS = (900_000, 1_070_000)
NYC_Y_BOUNDS = (120_000, 275_000)
NYC_BOROUGHS = {
    "BRONX",
    "BROOKLYN",
    "MANHATTAN",
    "QUEENS",
    "STATEN ISLAND",
    "UNSPECIFIED",
}
ZIP_RE = re.compile(r"^\d{5}$")
ZIP_MIN, ZIP_MAX = 10001, 11697
MIN_DATE = _dt.date(2010, 1, 1)
CONST_PAGE_SIZE = 50_000  # per-page when paging


class FetcherNYCOpenData(Fetcher):
    @staticmethod
    def validate(df: pd.DataFrame) -> pd.DataFrame:
        """
        Do NOT raise or drop. Annotate each row with:
        - DROP_FLAG: bool, True if any validation rule failed
        - DROP_REASON: semicolon-joined reasons that failed
        """
        df = df.copy()
        reason_masks: Dict[str, pd.Series] = {}

        def mark(cond: pd.Series, reason: str) -> None:
            if cond is None or cond.empty:
                return
            cond = cond.fillna(False)
            if cond.any():
                reason_masks[reason] = cond

        # --- Keys & duplicates (strict data integrity) ---
        if "unique_key" in df:
            mark(df["unique_key"].isna(), "unique_key null")
            mark(
                df["unique_key"].duplicated(keep="first"),
                "unique_key duplicate",
            )
        try:
            mark(
                _to_hashable_df(df).duplicated(keep="first"),
                "full-row duplicate",
            )
        except Exception as exc:
            # non-fatal diagnostic only
            reason_masks[f"duplicate check error: {exc!s}"] = pd.Series(
                False, index=df.index
            )

        # --- Dates (parse + ordering) ---
        for col in [
            "created_date",
            "closed_date",
            "resolution_action_updated_date",
        ]:
            if col in df:
                dt = _parse_dt(df[col])
                mark(dt.isna() & df[col].notna(), f"{col} unparsable")

        if {"created_date", "closed_date"} <= set(df.columns):
            cd = _parse_dt(df["created_date"])
            xd = _parse_dt(df["closed_date"])
            mark(
                (xd.notna()) & (cd.notna()) & (xd < cd),
                "closed_date before created_date",
            )

        if {"created_date", "resolution_action_updated_date"} <= set(
            df.columns
        ):
            cd = _parse_dt(df["created_date"])
            rd = _parse_dt(df["resolution_action_updated_date"])
            mark(
                (rd.notna()) & (cd.notna()) & (rd < cd),
                "resolution_action_updated_date before created_date",
            )

        # --- Status coherence (advisory) ---
        if "status" in df and "closed_date" in df:
            status_u = _nz(df["status"]).str.upper()
            is_closed = status_u.isin({"CLOSED", "RESOLVED"})
            has_closed_dt = _parse_dt(df["closed_date"]).notna()
            mark(
                (~is_closed) & has_closed_dt,
                "closed_date set but status not closed",
            )
            mark(
                is_closed & (~has_closed_dt),
                "closed status but closed_date missing",
            )

        # --- Geography (advisory) ---
        if "latitude" in df:
            lat = pd.to_numeric(df["latitude"], errors="coerce")
            mark(
                (lat < NYC_LAT_BOUNDS[0]) | (lat > NYC_LAT_BOUNDS[1]),
                "latitude outside NYC",
            )
        if "longitude" in df:
            lon = pd.to_numeric(df["longitude"], errors="coerce")
            mark(
                (lon < NYC_LON_BOUNDS[0]) | (lon > NYC_LON_BOUNDS[1]),
                "longitude outside NYC",
            )
        if {"latitude", "longitude"} <= set(df.columns):
            lat_na = df["latitude"].isna()
            lon_na = df["longitude"].isna()
            mark(lat_na ^ lon_na, "lat present xor lon present")

        if "x_coordinate_state_plane_" in df:
            x = pd.to_numeric(df["x_coordinate_state_plane_"], errors="coerce")
            mark(
                (x < NYC_X_BOUNDS[0]) | (x > NYC_X_BOUNDS[1]),
                "x_coordinate_state_plane_ out of bounds",
            )
        if "y_coordinate_state_plane_" in df:
            y = pd.to_numeric(df["y_coordinate_state_plane_"], errors="coerce")
            mark(
                (y < NYC_Y_BOUNDS[0]) | (y > NYC_Y_BOUNDS[1]),
                "y_coordinate_state_plane_ out of bounds",
            )

        # --- ZIP (advisory) ---
        if "incident_zip" in df:
            z = _nz(df["incident_zip"])
            mask_5 = z.str.fullmatch(ZIP_RE)
            z_num = pd.to_numeric(z.where(mask_5), errors="coerce")
            mark((~z.eq("")) & (~mask_5), "incident_zip not 5-digit")
            mark(
                mask_5 & ~z_num.between(ZIP_MIN, ZIP_MAX),
                "incident_zip outside NYC range",
            )

        # --- Borough (advisory) ---
        if "borough" in df:
            b = _nz(df["borough"]).str.upper()
            mark(~b.isin(NYC_BOROUGHS) & b.ne(""), "borough unexpected value")

        # Create DROP_FLAG and DROP_REASON (no rows dropped)
        if reason_masks:
            masks_df = pd.DataFrame(
                {
                    k: v.reindex(df.index).fillna(False)
                    for k, v in reason_masks.items()
                }
            )
            drop_flag = masks_df.any(axis=1)
            drop_reason = masks_df.apply(
                lambda r: "; ".join(r.index[r.values]), axis=1
            )
        else:
            drop_flag = pd.Series(False, index=df.index)
            drop_reason = pd.Series("", index=df.index, dtype="string")

        df["DROP_FLAG"] = drop_flag.values
        df["DROP_REASON"] = drop_reason.values

        # Compact summary
        total_flagged = int(drop_flag.sum())
        if total_flagged:
            counts = {
                reason: int(mask.sum()) for reason, mask in reason_masks.items()
            }
            parts = [f"{k}: {v}" for k, v in sorted(counts.items())]
            logger.info(
                f"Validation summary -> flagged {total_flagged} rows. "
                + "; ".join(parts)
            )
        else:
            logger.info("Validation summary -> no issues found.")

        return df

    @staticmethod
    def clean(df: pd.DataFrame) -> pd.DataFrame:
        for col in ["borough", "status", "agency", "complaint_type"]:
            if col in df:
                df[col] = df[col].astype(str).str.strip()
        return df

    @staticmethod
    def normalize(df: pd.DataFrame) -> pd.DataFrame:
        for col in [
            "created_date",
            "closed_date",
            "resolution_action_updated_date",
        ]:
            if col in df:
                df[col] = pd.to_datetime(df[col], errors="coerce").dt.strftime(
                    "%Y-%m-%d %H:%M:%S"
                )
        return df

    @classmethod
    def fetch(cls, config: Dict, as_of=None) -> pd.DataFrame:
        if not as_of:
            as_of = app_context.as_of
        token = config["token"]
        base_url = config["base_url"]  # e.g., "data.cityofnewyork.us"
        dataset = config["url"]  # e.g., "3rfa-3xsf"
        query_kwargs = {
            k: v for k, v in config.get("kwargs", {}).items() if v is not None
        }
        client = Socrata(base_url, app_token=token)

        if not query_kwargs:
            logger.info(f"added filter: created_date = {as_of}")
            query_kwargs["where"] = (
                f"created_date = '{ul.date_as_iso_format(as_of)}'"
            )
            page_size = int(config.get("page_size", CONST_PAGE_SIZE))
            paged_kwargs = dict(query_kwargs)
            paged_kwargs["limit"] = page_size
            rows = list(client.get_all(dataset, **paged_kwargs))
        else:
            if "limit" not in query_kwargs:
                page_size = int(config.get("page_size", CONST_PAGE_SIZE))
                paged_kwargs = dict(query_kwargs)
                paged_kwargs["limit"] = page_size
                rows = list(client.get_all(dataset, **paged_kwargs))
            else:
                rows = client.get(dataset, **query_kwargs)

        df = pd.DataFrame.from_records(rows)
        logger.info(f"Fetched {len(df)} rows")
        return df

    @staticmethod
    def finalize(df: pd.DataFrame) -> pd.DataFrame:
        if "DROP_REASON" in df.columns:
            df["DROP_REASON"] = df["DROP_REASON"].fillna("")
        return df
