import datetime
import json
import math
import tempfile
from collections import namedtuple
from pathlib import Path
from typing import Any

import pandas as pd
import yaml
from IPython import display


def get_config(config_name: str) -> dict[str, Any] | list[dict[str, Any]]:
    base = Path(__file__).resolve().parent / "ltp" / "configs"
    path = (
        (base / config_name)
        if Path(config_name).suffix
        else (base / f"{config_name}.yaml")
    )
    if not path.exists() and not Path(config_name).suffix:  # try .yml fallback
        path = base / f"{config_name}.yml"
    if not path.exists():
        raise FileNotFoundError(f"Config not found: {path}")
    with path.open("r", encoding="utf-8") as f:
        docs = [d for d in yaml.safe_load_all(f) if d is not None]
    if not docs:
        raise ValueError(f"Config is empty: {path}")
    return docs[0] if len(docs) == 1 else docs


def get_dir_base() -> Path:
    base = Path(tempfile.gettempdir()) / "hbc_nyc_dp"
    base.mkdir(parents=True, exist_ok=True)
    return base


def get_dir_cache(postfix: str | None = None) -> Path:
    base = get_dir_base() / "CACHE"
    cache = base / postfix if postfix else base
    cache.mkdir(parents=True, exist_ok=True)
    return cache


def get_dir_analytics() -> Path:
    analytics = get_dir_base() / "ANALYTICS"
    analytics.mkdir(parents=True, exist_ok=True)
    return analytics


def mk_dir(path: Path) -> Path:
    path.mkdir(parents=True, exist_ok=True)
    return path


def date_as_str(dt: datetime.date, format="%Y%m%d") -> str:
    if isinstance(dt, str):
        return dt
    return dt.strftime(format)


def date_as_iso_format(dt: datetime.date) -> str:
    return f"{dt.isoformat()}T00:00:00"


def str_as_date(dt: str):
    if dt is None:
        return
    if isinstance(dt, datetime.date):
        return dt
    return pd.to_datetime(dt).date()


def _parse_dt(s: pd.Series) -> pd.Series:
    return pd.to_datetime(s, errors="coerce")


def _nz(series: pd.Series) -> pd.Series:
    # why: treat empty/whitespace as missing for text fields
    return series.fillna("").astype(str).str.strip()


def _jsonify_unhashable(x: Any) -> Any:
    # why: make dict/list/tuple/set hashable for duplicate checks
    if x is None or (isinstance(x, float) and math.isnan(x)):
        return None
    if isinstance(x, (dict, list, tuple, set)):
        try:
            return json.dumps(x, sort_keys=True, ensure_ascii=False)
        except Exception:
            return str(x)
    return x


def _to_hashable_df(df: pd.DataFrame) -> pd.DataFrame:
    out = df.copy()
    obj_cols = [c for c in out.columns if out[c].dtype == "O"]
    for c in obj_cols:
        out[c] = out[c].map(_jsonify_unhashable)
    return out


def display_full_df(df):
    if isinstance(df, pd.Series):
        df = df.to_frame()

    pd.set_option("display.max_rows", pd.DataFrame(df).shape[0])
    pd.set_option("display.max_columns", pd.DataFrame(df).shape[1])
    pd.set_option("display.max_colwidth", 1000)
    display.display(pd.DataFrame(df))

    pd.reset_option("display.max_rows")
    pd.reset_option("display.max_columns")
    pd.reset_option("display.max_colwidth")


def to_namedtuple(d, recursive=True):
    if isinstance(d, dict):
        d = d.copy()
        if recursive:
            for k, v in d.items():
                d[k] = to_namedtuple(v, recursive)
        d = namedtuple("_", d.keys())(**d)

    return d


def cols_as_named_tuple(df: pd.DataFrame):
    dct_col = dict(zip(df.columns, df.columns))
    return to_namedtuple(dct_col)


def pretty_columns_names(df):
    df.columns = [
        val.split("[")[0].rstrip() if "[" in val else val
        for val in df.columns.values.tolist()
    ]
    df.columns = [val.lower() for val in df.columns.tolist()]
    df.columns = [val.replace("\n", "") for val in df.columns.tolist()]
    df.columns = [val.replace(" ", "_") for val in df.columns.tolist()]
    df.columns = [val.replace("(", "") for val in df.columns.tolist()]
    df.columns = [val.replace(")", "") for val in df.columns.tolist()]
    df.columns = [val.replace("&", "") for val in df.columns.tolist()]


# file: hbc/quant/io_excel.py
import re
from pathlib import Path

import pandas as pd


def _sheetify(name):
    # Excel forbids : \ / ? * [ ] and max 31 chars
    clean = re.sub(r"[:\\/?*\[\]]", "_", str(name)).strip() or "Sheet1"
    return clean[:31]


def _autofit_columns(xlsx_path, sheet_name, max_width=80):
    # Set column widths to max text length in the column (capped)
    from openpyxl import load_workbook

    wb = load_workbook(xlsx_path)
    ws = wb[sheet_name]
    for col_cells in ws.columns:
        # header-aware length
        lengths = []
        for cell in col_cells:
            v = "" if cell.value is None else str(cell.value)
            lengths.append(len(v))
        width = min(max(lengths, default=0) + 2, max_width)
        ws.column_dimensions[col_cells[0].column_letter].width = width
    wb.save(xlsx_path)
    wb.close()


def save_dataframe_as_sheet(
    dir_path,
    filename,
    df,
    sheet_name,
    replace=False,
    index=True,
    autofit=True,
    max_width=80,
):
    """
    Append DataFrame as a sheet to an Excel file, creating the file if needed.
    - replace=True overwrites an existing sheet of the same name.
    - autofit=True adjusts column widths after writing.
    """
    out_dir = Path(dir_path)
    out_dir.mkdir(parents=True, exist_ok=True)
    xlsx = out_dir / filename

    sheet = _sheetify(sheet_name)
    mode = "a" if xlsx.exists() else "w"

    if mode == "a":
        from openpyxl import load_workbook

        wb = load_workbook(xlsx)
        existing = set(wb.sheetnames)
        wb.close()

        if replace and sheet in existing:
            with pd.ExcelWriter(
                xlsx, engine="openpyxl", mode="a", if_sheet_exists="replace"
            ) as w:
                df.to_excel(w, sheet_name=sheet, index=index)
            if autofit:
                _autofit_columns(xlsx, sheet, max_width=max_width)
            return

        base = sheet
        i = 1
        while sheet in existing:
            suffix = f"_{i}"
            sheet = _sheetify(base[: 31 - len(suffix)] + suffix)
            i += 1

    with pd.ExcelWriter(xlsx, engine="openpyxl", mode=mode) as w:
        df.to_excel(w, sheet_name=sheet, index=index)

    if autofit:
        _autofit_columns(xlsx, sheet, max_width=max_width)
