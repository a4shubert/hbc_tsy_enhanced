# file: hbc/quant/analysis.py
import pandas as pd


class AnalyticalEngine:
    """Basic analytics helpers for ranking and aggregating metrics."""

    @staticmethod
    def _validate_inputs(df, col_metric, group):
        """Ensure required columns exist before performing analytics."""
        cols = [col_metric] + (group or [])
        missing = [c for c in cols if c not in df.columns]
        if missing:
            raise KeyError("Missing columns: %r" % missing)

    @classmethod
    def top_n_best(cls, n, df, col_metric, group=None):
        """Return smallest metric values overall or per group."""
        # ensure numeric if it accidentally became strings
        if df[col_metric].dtype == "object":
            df = df.copy()
            df[col_metric] = pd.to_numeric(df[col_metric], errors="coerce")

        if group:
            s = (
                df[group + [col_metric]]
                .dropna(subset=[col_metric])
                .groupby(group, observed=True, dropna=False)[col_metric]
                .min()
            )
            return (
                s.sort_values(ascending=True).head(n).to_frame(name=col_metric)
            )

        base = df[[col_metric]].dropna(subset=[col_metric])
        # nsmallest already returns ascending; keep it ascending
        return base.nsmallest(n, col_metric).sort_values(
            col_metric, ascending=True
        )

    @classmethod
    def top_n_worst(cls, n, df, col_metric, group=None):
        """Return largest metric values overall or per group."""
        cls._validate_inputs(df, col_metric, group)
        if group:
            s = (
                df[group + [col_metric]]
                .dropna(subset=[col_metric])
                .groupby(group, observed=True, dropna=False)[col_metric]
                .max()
            )
            out = s.to_frame(name=col_metric)
            return out.sort_values(col_metric, ascending=False).head(n)
        base = df[[col_metric]].dropna(subset=[col_metric])
        return base.nlargest(n, col_metric).sort_values(
            col_metric, ascending=False
        )

    @classmethod
    def median(cls, df, col_metric, group=None):
        """Compute median metric overall or grouped."""
        cls._validate_inputs(df, col_metric, group)
        if group:
            s = (
                df[group + [col_metric]]
                .dropna(subset=[col_metric])
                .groupby(group, observed=True, dropna=False)[col_metric]
                .median()
            )
            return s.to_frame(name=col_metric).sort_values(
                col_metric, ascending=False
            )
        val = df[col_metric].median(skipna=True)
        return pd.DataFrame({col_metric: [float(val)]}).sort_values(
            col_metric, ascending=False
        )

    @classmethod
    def mean(cls, df, col_metric, group=None):
        """Compute mean metric overall or grouped."""
        cls._validate_inputs(df, col_metric, group)
        if group:
            s = (
                df[group + [col_metric]]
                .dropna(subset=[col_metric])
                .groupby(group, observed=True, dropna=False)[col_metric]
                .mean()
            )
            return s.to_frame(name=col_metric).sort_values(
                col_metric, ascending=False
            )
        val = df[col_metric].mean(skipna=True)
        return pd.DataFrame({col_metric: [float(val)]}).sort_values(
            col_metric, ascending=False
        )

    @classmethod
    def descriptive_stats(cls, n_best, n_worst, df, col_metric, group=None):
        """Return dict of best/worst/median/mean tables for a metric."""
        n_best = int(n_best)
        n_worst = int(n_worst)
        cls._validate_inputs(df, col_metric, group)
        return {
            "best": cls.top_n_best(n_best, df, col_metric, group=group),
            "worst": cls.top_n_worst(n_worst, df, col_metric, group=group),
            "median": cls.median(df, col_metric, group=group),
            "mean": cls.mean(df, col_metric, group=group),
        }
