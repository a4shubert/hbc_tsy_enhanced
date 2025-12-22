import datetime
import shutil
import unittest
from pathlib import Path
from types import SimpleNamespace
from unittest import mock

import pandas as pd

from hbc import app_context
from hbc.ltp.persistence.cache import Cache
import hbc.utils as ul


class TestPersistence(unittest.TestCase):
    TMP_ROOT = Path(__file__).resolve().parent / "tmp"

    def setUp(self):
        shutil.rmtree(self.TMP_ROOT, ignore_errors=True)
        self.TMP_ROOT.mkdir(parents=True, exist_ok=True)
        self._patcher = mock.patch("hbc.utils.get_dir_base", lambda: self.TMP_ROOT)
        self._patcher.start()
        # refresh derived paths after monkeypatch
        app_context.dir_cache = ul.get_dir_cache()

    def tearDown(self):
        self._patcher.stop()
        shutil.rmtree(self.TMP_ROOT, ignore_errors=True)

    def test_persistence_roundtrip(self):
        dc = SimpleNamespace(
            moniker="demo_ds",
            df=pd.DataFrame({"a": [1, 2], "b": ["x", "y"]}),
        )
        as_of = datetime.date(2020, 1, 1)

        Cache.to_cache(dc, as_of=as_of)
        df_loaded = Cache.from_cache(dc, as_of=as_of)

        self.assertFalse(df_loaded.empty)
        self.assertEqual(list(df_loaded.columns), ["a", "b"])
        self.assertEqual(df_loaded.iloc[0]["a"], 1)

        dates = Cache.get_all_cached_dates(dc)
        self.assertEqual(dates, [ul.date_as_str(as_of)])
