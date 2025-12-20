# path: plotting/plot_engine.py
"""
PlotEngine
==========

Quick plotting helpers:
- plot_ts: time series of counts/aggregations with optional trend (static, Matplotlib).
- plot_bar: grouped bars with % options (static, Matplotlib).
- plot_geo_spatial: static geo “bubble” scatter on axes (no map tiles).
- plot_geo_map: interactive Leaflet map (Folium) with bubbles on real map tiles.
- clean_lat_lon: sanitize latitude/longitude columns.

Notes
-----
- `plot_geo_map` saves an HTML file that fetches tiles (e.g., OpenStreetMap) when opened.
  This requires internet *in your browser* (not during Python execution). For Google base
  maps, use Google Maps JS SDK (not provided via Folium tiles) and a valid API key.
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import Dict, Iterable, List, Literal, Optional, Union, Sequence, Tuple

import matplotlib.pyplot as plt
import numpy as np
import pandas as pd

AggType = Literal["count", "sum", "mean", "median", "min", "max"]
TrendMethod = Literal["rolling", "ema"]


@dataclass
class _SizeScale:
    """
    Size scaler for geo bubbles (static Matplotlib usage).

    Values are min-max normalized to [0, 1], then transformed by `value**power`
    to dampen outliers (e.g., sqrt when power=0.5).
    """
    min_size: float = 20.0
    max_size: float = 1000.0
    power: float = 0.5

    def scale(self, values: pd.Series) -> pd.Series:
        v = values.astype(float).clip(lower=0)
        if v.empty or v.max() == v.min():
            mid = (self.min_size + self.max_size) / 2
            return pd.Series(np.full(len(v), mid), index=v.index)
        v_norm = (v - v.min()) / (v.max() - v.min())
        return self.min_size + (self.max_size - self.min_size) * (v_norm ** self.power)


class PlotEngine:
    """Static/class plotting helpers for time series, bar charts, and geo maps."""

    # ---------------- Utilities ----------------

    @staticmethod
    def _ensure_columns(df: pd.DataFrame, cols: Iterable[str]) -> None:
        """Raise KeyError if any column in `cols` is missing from `df`."""
        missing = [c for c in cols if c not in df.columns]
        if missing:
            raise KeyError(f"Missing required column(s): {missing}")

    @staticmethod
    def _to_datetime(df: pd.DataFrame, col_time: str) -> pd.DataFrame:
        """Coerce `col_time` to datetime, drop NaT rows, and sort ascending."""
        out = df.copy()
        out[col_time] = pd.to_datetime(out[col_time], errors="coerce", utc=False)
        out = out.dropna(subset=[col_time]).sort_values(col_time)
        return out

    @staticmethod
    def _apply_filters(
        df: pd.DataFrame, filter_by: Optional[Dict[str, Union[object, Iterable[object]]]]
    ) -> pd.DataFrame:
        """
        Apply equality/isin filters mapping column -> value or iterable of values.
        """
        if not filter_by:
            return df
        mask = pd.Series(True, index=df.index)
        for col, val in filter_by.items():
            if isinstance(val, Iterable) and not isinstance(val, (str, bytes)):
                mask &= df[col].isin(list(val))
            else:
                mask &= df[col].eq(val)
        return df.loc[mask]

    @staticmethod
    def clean_lat_lon(
        df: pd.DataFrame,
        col_latitude: str,
        col_longitude: str,
        *,
        drop_out_of_bounds: bool = True,
        drop_zeros: bool = True,
    ) -> pd.DataFrame:
        """
        Convert latitude/longitude to float and drop invalid/empty rows.

        Steps
        -----
        1) `pd.to_numeric(..., errors="coerce")` on both columns.
        2) Drop NaNs (handles '', None, 'NA', etc.).
        3) Optionally drop out-of-bounds: lat∈[-90,90], lon∈[-180,180].
        4) Optionally drop rows where both lat & lon equal 0.0.

        Returns
        -------
        pd.DataFrame
            Cleaned copy containing valid numeric coordinates only.
        """
        PlotEngine._ensure_columns(df, [col_latitude, col_longitude])
        out = df.copy()
        out[col_latitude] = pd.to_numeric(out[col_latitude], errors="coerce")
        out[col_longitude] = pd.to_numeric(out[col_longitude], errors="coerce")
        out = out.dropna(subset=[col_latitude, col_longitude])

        if drop_out_of_bounds:
            lat_ok = out[col_latitude].between(-90.0, 90.0)
            lon_ok = out[col_longitude].between(-180.0, 180.0)
            out = out[lat_ok & lon_ok]

        if drop_zeros:
            not_zero_pair = ~((out[col_latitude] == 0.0) & (out[col_longitude] == 0.0))
            out = out[not_zero_pair]

        return out

    # ---------------- Time Series (static) ----------------

    @classmethod
    def plot_ts(
        cls,
        df: pd.DataFrame,
        col_time: str,
        col_metric: Optional[str] = None,
        *,
        add_trend: bool = True,
        trend_window: Optional[int] = None,
        trend_method: TrendMethod = "ema",
        trend_min_periods: int = 1,
        freq: str = "D",
        aggregation: AggType = "count",
        filter_by: Optional[Dict[str, Union[object, Iterable[object]]]] = None,
        title: Optional[str] = None,
        ylabel: Optional[str] = None,
        ax: Optional[plt.Axes] = None,
        show: bool = True,
        savepath: Optional[str] = None,
    ) -> pd.DataFrame:
        """
        Plot a time series of counts (default) or aggregated metric per period.

        Returns
        -------
        pd.DataFrame
            DatetimeIndex + 'value' column (aggregated series).
        """
        cls._ensure_columns(df, [col_time])
        if col_metric is not None:
            cls._ensure_columns(df, [col_metric])

        df_f = cls._apply_filters(df, filter_by)
        df_t = cls._to_datetime(df_f, col_time)

        if aggregation == "count" or col_metric is None:
            s = (
                df_t.set_index(col_time)
                .assign(_one=1)
                .resample(freq)["_one"]
                .sum()
                .astype(int)
            )
        else:
            s = df_t.set_index(col_time)[col_metric].resample(freq).agg(aggregation)

        out = pd.DataFrame({"value": s})
        out.index.name = "time"

        if ax is None:
            _, ax = plt.subplots(figsize=(10, 5))

        ax.plot(out.index, out["value"], linewidth=2, label="Series")

        if add_trend:
            win = trend_window if (trend_window and trend_window > 1) else 7
            if trend_method == "rolling":
                trend = out["value"].rolling(window=win, min_periods=trend_min_periods).mean().bfill()
                label = f"Trend (Rolling {win})"
            else:
                trend = out["value"].ewm(span=win, adjust=False).mean()
                label = f"Trend (EMA {win})"
            ax.plot(out.index, trend, linestyle="--", linewidth=2, label=label)

        filt_descr = ""
        if filter_by:
            parts = [f"{k}={v}" for k, v in filter_by.items()]
            filt_descr = " | " + ", ".join(parts)
        ax.set_title(title or f"Time Series ({aggregation}){filt_descr}")
        ax.set_xlabel("Time")
        ax.set_ylabel(ylabel or ("Count" if aggregation == "count" or col_metric is None else aggregation.title()))
        ax.grid(True, alpha=0.3)
        ax.legend()

        if savepath:
            plt.savefig(savepath, bbox_inches="tight", dpi=150)
        if show:
            plt.show()
        return out

    # ---------------- Bar Chart (static) ----------------

    @classmethod
    def plot_bar(
        cls,
        df: pd.DataFrame,
        group_cols: Union[str, List[str]],
        *,
        col_metric: Optional[str] = None,
        aggregation: AggType = "count",
        filter_by: Optional[Dict[str, Union[object, Iterable[object]]]] = None,
        top_n: Optional[int] = None,
        sort_by_value: bool = True,
        ascending: bool = False,
        largest_on_top=True,
        percent: bool = False,
        percent_base: Literal["plotted", "all"] = "plotted",
        annotate: bool = True,
        orient: Literal["v", "h"] = "v",
        title: Optional[str] = None,
        xlabel: Optional[str] = None,
        ylabel: Optional[str] = None,
        ax: Optional[plt.Axes] = None,
        show: bool = True,
        savepath: Optional[str] = None,
        rotation: int = 0,
    ) -> pd.DataFrame:
        """
        Bar chart of counts (default) or aggregated metric per group(s).

        Notes
        -----
        - `percent=True` with `percent_base="plotted"` normalizes only the shown bars.
        - Use `percent_base="all"` for share of the entire population before slicing.

        Returns
        -------
        pd.DataFrame
            Group columns + 'value'.
        """
        groups = [group_cols] if isinstance(group_cols, str) else list(group_cols)
        cls._ensure_columns(df, groups)
        if col_metric is not None and aggregation != "count":
            cls._ensure_columns(df, [col_metric])

        dff = cls._apply_filters(df, filter_by)

        if aggregation == "count" or col_metric is None:
            grouped = dff.groupby(groups).size().rename("value")
        else:
            grouped = dff.groupby(groups)[col_metric].agg(aggregation).rename("value")

        out = grouped.reset_index()
        if sort_by_value:
            out = out.sort_values("value", ascending=ascending)
        if top_n is not None and top_n > 0:
            out = out.head(top_n)

        if percent:
            denom = grouped.sum() if percent_base == "all" else out["value"].sum()
            if denom > 0:
                out["value"] = out["value"] / denom * 100.0

        out["label"] = (
            out[groups[0]].astype(str)
            if len(groups) == 1
            else out[groups].astype(str).agg(" | ".join, axis=1)
        )

        if ax is None:
            _, ax = plt.subplots(figsize=(10, 6))

        if orient == "h":
            ax.barh(out["label"], out["value"])
            ax.set_xlabel(xlabel or ("Percent" if percent else ("Value" if aggregation != "count" else "Count")))
            ax.set_ylabel(ylabel or "Group")

            # --- NEW: put largest at the top when descending ---
            if largest_on_top is None:
                largest_on_top = (not ascending)  # auto behavior
            if largest_on_top:
                ax.invert_yaxis()

        else:
            ax.bar(out["label"], out["value"])
            ax.set_ylabel(ylabel or ("Percent" if percent else ("Value" if aggregation != "count" else "Count")))
            ax.set_xlabel(xlabel or "Group")
            if rotation:
                ax.set_xticklabels(out["label"], rotation=rotation, ha="right")

        filt_descr = ""
        if filter_by:
            parts = [f"{k}={v}" for k, v in filter_by.items()]
            filt_descr = " | " + ", ".join(parts)
        ax.set_title(title or f"Bar Chart ({aggregation}{' %' if percent else ''}){filt_descr}")
        ax.grid(True, axis="x" if orient == "h" else "y", alpha=0.3)

        if annotate:
            if orient == "h":
                for container in ax.containers:
                    for rect in container:
                        width = rect.get_width()
                        if np.isnan(width):
                            continue
                        ax.annotate(
                            f"{width:.1f}%" if percent else f"{int(width) if float(width).is_integer() else round(width, 2)}",
                            xy=(width, rect.get_y() + rect.get_height() / 2),
                            xytext=(5, 0),
                            textcoords="offset points",
                            va="center",
                        )
            else:
                for container in ax.containers:
                    for rect in container:
                        height = rect.get_height()
                        if np.isnan(height):
                            continue
                        ax.annotate(
                            f"{height:.1f}%" if percent else f"{int(height) if float(height).is_integer() else round(height, 2)}",
                            xy=(rect.get_x() + rect.get_width() / 2, height),
                            xytext=(0, 3),
                            textcoords="offset points",
                            ha="center",
                            va="bottom",
                        )

        if savepath:
            plt.savefig(savepath, bbox_inches="tight", dpi=150)
        if show:
            plt.show()

        return out[groups + ["value"]]

    # ---------------- Geo Bubble (static) ----------------

    @classmethod
    def plot_geo_spatial(
        cls,
        df: pd.DataFrame,
        col_latitude: str,
        col_longitude: str,
        col_metric: Optional[str] = None,
        *,
        aggregation: AggType = "count",
        round_precision: int = 2,
        size_scale: _SizeScale = _SizeScale(),
        annotate_top_n: int = 0,
        ax: Optional[plt.Axes] = None,
        show: bool = True,
        savepath: Optional[str] = None,
        title: Optional[str] = None,
        clean: bool = True,
        drop_out_of_bounds: bool = True,
        drop_zeros: bool = True,
    ) -> pd.DataFrame:
        """
        Static bubble plot (no basemap). Each bubble is an aggregated value at rounded lat/lon.
        """
        cls._ensure_columns(df, [col_latitude, col_longitude])
        if col_metric is not None and aggregation != "count":
            cls._ensure_columns(df, [col_metric])

        if clean:
            dfg = cls.clean_lat_lon(
                df, col_latitude, col_longitude,
                drop_out_of_bounds=drop_out_of_bounds,
                drop_zeros=drop_zeros,
            )
        else:
            dfg = df.dropna(subset=[col_latitude, col_longitude]).copy()

        dfg["lat_r"] = dfg[col_latitude].round(round_precision)
        dfg["lon_r"] = dfg[col_longitude].round(round_precision)

        if aggregation == "count" or col_metric is None:
            agg = dfg.groupby(["lat_r", "lon_r"]).size().rename("value")
        else:
            agg = dfg.groupby(["lat_r", "lon_r"])[col_metric].agg(aggregation).rename("value")

        agg = agg.reset_index().rename(columns={"lat_r": "lat", "lon_r": "lon"})
        agg["size"] = size_scale.scale(agg["value"])

        if ax is None:
            _, ax = plt.subplots(figsize=(8, 6))

        ax.scatter(agg["lon"], agg["lat"], s=agg["size"], alpha=0.6, edgecolor="white", linewidth=0.5)
        ax.set_xlabel("Longitude")
        ax.set_ylabel("Latitude")
        ax.set_title(title or f"Geo Bubble Map ({aggregation}) (rounded {round_precision} dp)")
        ax.grid(True, alpha=0.2)
        try:
            ax.set_aspect("equal", adjustable="datalim")
        except Exception:
            pass

        if annotate_top_n > 0 and not agg.empty:
            top = agg.nlargest(annotate_top_n, "value")
            for _, r in top.iterrows():
                label = int(r["value"]) if float(r["value"]).is_integer() else round(r["value"], 2)
                ax.annotate(f"{label}", (r["lon"], r["lat"]), xytext=(3, 3), textcoords="offset points", fontsize=8, alpha=0.8)

        if savepath:
            plt.savefig(savepath, bbox_inches="tight", dpi=150)
        if show:
            plt.show()
        return agg

    # ---------------- Geo Map (interactive, Folium) ----------------

    @classmethod
    def plot_geo_map(
        cls,
        df: pd.DataFrame,
        col_latitude: str,
        col_longitude: str,
        *,
        col_metric: Optional[str] = None,
        aggregation: AggType = "count",
        filter_by: Optional[Dict[str, Union[object, Iterable[object]]]] = None,
        round_precision: int = 3,
        cluster: bool = True,
        popup_cols: Optional[Sequence[str]] = None,
        min_radius: float = 3.0,
        max_radius: float = 20.0,
        start_location: Optional[Tuple[float, float]] = None,
        start_zoom: int = 11,
        tiles: str = "OpenStreetMap",
        tiles_url: Optional[str] = None,
        tiles_attr: Optional[str] = None,
        savepath: str = "requests_map.html",
        clean: bool = True,
        drop_out_of_bounds: bool = True,
        drop_zeros: bool = True,
    ) -> pd.DataFrame:
        """
        Create an interactive Leaflet map with bubbles sized by counts/metric.

        Parameters
        ----------
        df : pd.DataFrame
            Source data.
        col_latitude, col_longitude : str
            Coordinate column names.
        col_metric : str | None
            Metric to aggregate. When `None` and aggregation='count', bubbles show counts.
        aggregation : {'count','sum','mean','median','min','max'}
            Aggregation applied per rounded coordinate.
        filter_by : dict | None
            Optional equality/isin filters prior to cleaning/aggregation.
        round_precision : int
            Decimal places used to round lat/lon to merge nearby points.
        cluster : bool
            Wrap markers in a `MarkerCluster` for readability.
        popup_cols : Sequence[str] | None
            Columns to display in marker popups (best for non-aggregated views).
            For aggregated points, popup shows lat/lon and aggregated value.
        min_radius, max_radius : float
            Circle radii bounds in pixels; actual radius scales with sqrt(values).
        start_location : (lat, lon) | None
            Map center; if None, center on data centroid.
        start_zoom : int
            Initial zoom level.
        tiles : str
            Built-in Folium tiles name (e.g., 'OpenStreetMap', 'CartoDB positron').
        tiles_url, tiles_attr : str | None
            Custom tile URL template + attribution (overrides `tiles`).
        savepath : str
            Output HTML file path.
        clean, drop_out_of_bounds, drop_zeros : bool
            Sanitize coordinates via `clean_lat_lon` before plotting.

        Returns
        -------
        pd.DataFrame
            Aggregated table with columns ['lat','lon','value'] used for bubbles.

        Notes
        -----
        - The saved HTML loads tiles over the internet when viewed.
        - Google base maps require Google Maps JS SDK, not Folium tiles.
        """
        # Import here to keep Folium optional if user only needs Matplotlib APIs.
        import folium
        from folium.plugins import MarkerCluster

        cls._ensure_columns(df, [col_latitude, col_longitude])
        if col_metric is not None and aggregation != "count":
            cls._ensure_columns(df, [col_metric])

        dff = cls._apply_filters(df, filter_by)

        if clean:
            dff = cls.clean_lat_lon(
                dff, col_latitude, col_longitude,
                drop_out_of_bounds=drop_out_of_bounds,
                drop_zeros=drop_zeros,
            )
        else:
            dff = dff.dropna(subset=[col_latitude, col_longitude]).copy()

        # Aggregate by rounded coords to avoid thousands of overlapping markers
        dff["lat_r"] = dff[col_latitude].round(round_precision)
        dff["lon_r"] = dff[col_longitude].round(round_precision)

        if aggregation == "count" or col_metric is None:
            agg = dff.groupby(["lat_r", "lon_r"]).size().rename("value")
        else:
            agg = dff.groupby(["lat_r", "lon_r"])[col_metric].agg(aggregation).rename("value")

        agg = agg.reset_index().rename(columns={"lat_r": "lat", "lon_r": "lon"})

        if agg.empty:
            # Create an empty map near (0,0) to avoid crashing.
            center = start_location or (0.0, 0.0)
            m = folium.Map(location=center, zoom_start=start_zoom, tiles=tiles if tiles_url is None else None, control_scale=True)
            if tiles_url:
                folium.TileLayer(tiles=tiles_url, attr=tiles_attr or "").add_to(m)
            m.save(savepath)
            return agg

        # Scale radius with sqrt to avoid huge bubbles
        v = agg["value"].astype(float).clip(lower=0)
        if v.max() == v.min():
            radii = np.full(len(v), (min_radius + max_radius) / 2.0)
        else:
            v_norm = (v - v.min()) / (v.max() - v.min())
            radii = min_radius + (max_radius - min_radius) * np.sqrt(v_norm)
        agg["radius"] = radii

        # Choose center
        if start_location is None:
            center = (float(agg["lat"].mean()), float(agg["lon"].mean()))
        else:
            center = start_location

        # Create map and add tiles
        m = folium.Map(location=center, zoom_start=start_zoom, tiles=tiles if tiles_url is None else None, control_scale=True)
        if tiles_url:
            folium.TileLayer(tiles=tiles_url, attr=tiles_attr or "").add_to(m)

        # Add markers
        if cluster:
            cluster_layer = MarkerCluster().add_to(m)
            target = cluster_layer
        else:
            target = m

        # Build popups: aggregated view shows count/value; if popup_cols provided and data
        # is not heavily aggregated, you can instead plot *raw* rows (not recommended at scale).
        for _, r in agg.iterrows():
            lat, lon, val, rad = float(r["lat"]), float(r["lon"]), r["value"], float(r["radius"])
            popup_html = folium.Html(f"<b>Value:</b> {val}<br><b>Lat:</b> {lat:.6f}<br><b>Lon:</b> {lon:.6f}", script=True)
            folium.CircleMarker(
                location=(lat, lon),
                radius=rad,  # pixels
                fill=True,
                fill_opacity=0.6,
                opacity=0.8,
                # Reason: keep style simple and readable.
                color=None,
            ).add_to(target).add_child(folium.Popup(popup_html, max_width=300))

        # Save output
        m.save(savepath)
        return agg


