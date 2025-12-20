# file: hbc/quant/analysis.py
import pandas as pd


class AnalyticalEngine:
    @staticmethod
    def _validate_inputs(df, metric_col, group):
        cols = [metric_col] + (group or [])
        missing = [c for c in cols if c not in df.columns]
        if missing:
            raise KeyError("Missing columns: %r" % missing)

    @classmethod
    def top_n_best(cls, n, df, metric_col, group=None):
        # ensure numeric if it accidentally became strings
        if df[metric_col].dtype == "object":
            df = df.copy()
            df[metric_col] = pd.to_numeric(df[metric_col], errors="coerce")

        if group:
            s = (
                df[group + [metric_col]]
                .dropna(subset=[metric_col])
                .groupby(group, observed=True, dropna=False)[metric_col]
                .min()
            )
            return (
                s.sort_values(ascending=True).head(n).to_frame(name=metric_col)
            )

        base = df[[metric_col]].dropna(subset=[metric_col])
        # nsmallest already returns ascending; keep it ascending
        return base.nsmallest(n, metric_col).sort_values(
            metric_col, ascending=True
        )

    @classmethod
    def top_n_worst(cls, n, df, metric_col, group=None):
        cls._validate_inputs(df, metric_col, group)
        if group:
            s = (
                df[group + [metric_col]]
                .dropna(subset=[metric_col])
                .groupby(group, observed=True, dropna=False)[metric_col]
                .max()
            )
            out = s.to_frame(name=metric_col)
            return out.sort_values(metric_col, ascending=False).head(n)
        base = df[[metric_col]].dropna(subset=[metric_col])
        return base.nlargest(n, metric_col).sort_values(
            metric_col, ascending=False
        )

    @classmethod
    def median(cls, df, metric_col, group=None):
        cls._validate_inputs(df, metric_col, group)
        if group:
            s = (
                df[group + [metric_col]]
                .dropna(subset=[metric_col])
                .groupby(group, observed=True, dropna=False)[metric_col]
                .median()
            )
            return s.to_frame(name=metric_col).sort_values(
                metric_col, ascending=False
            )
        val = df[metric_col].median(skipna=True)
        return pd.DataFrame({metric_col: [float(val)]}).sort_values(
            metric_col, ascending=False
        )

    @classmethod
    def mean(cls, df, metric_col, group=None):
        cls._validate_inputs(df, metric_col, group)
        if group:
            s = (
                df[group + [metric_col]]
                .dropna(subset=[metric_col])
                .groupby(group, observed=True, dropna=False)[metric_col]
                .mean()
            )
            return s.to_frame(name=metric_col).sort_values(
                metric_col, ascending=False
            )
        val = df[metric_col].mean(skipna=True)
        return pd.DataFrame({metric_col: [float(val)]}).sort_values(
            metric_col, ascending=False
        )

    @classmethod
    def descriptive_stats(cls, n_best, n_worst, df, metric_col, group=None):
        n_best = int(n_best)
        n_worst = int(n_worst)
        cls._validate_inputs(df, metric_col, group)
        return {
            "best": cls.top_n_best(n_best, df, metric_col, group=group),
            "worst": cls.top_n_worst(n_worst, df, metric_col, group=group),
            "median": cls.median(df, metric_col, group=group),
            "mean": cls.mean(df, metric_col, group=group),
        }
