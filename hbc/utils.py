import datetime
import tempfile
from pathlib import Path
from typing import Any

import pandas as pd
import yaml


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


def get_base_dir() -> Path:
    base = Path(tempfile.gettempdir()) / "hbc_nyc_dp"
    base.mkdir(parents=True, exist_ok=True)
    return base


def get_cache_dir(postfix: str | None = None) -> Path:
    base = get_base_dir() / "CACHE"
    cache = base / postfix if postfix else base
    cache.mkdir(parents=True, exist_ok=True)
    return cache


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
    if isinstance(dt, datetime.date):
        return dt
    return pd.to_datetime(dt).date()
