import shutil
import unittest
from pathlib import Path
from unittest import mock

import pandas as pd
from pandas.testing import assert_frame_equal

from hbc.jobs.job_analytics import job_analysis_nyc_311
import hbc.utils as ul


class TestJobAnalysisNYC311(unittest.TestCase):
    TMP_ROOT = Path(__file__).resolve().parent / "tmp"
    BENCH_ROOT = Path(__file__).resolve().parent / "benchmark"
    MONIKER = "nyc_open_data_311_service_requests"
    AS_OF_STR = "20091231"

    def setUp(self):
        self.TMP_ROOT.mkdir(parents=True, exist_ok=True)
        self.baseline_input = self.BENCH_ROOT / f"{self.MONIKER}.csv"
        self.baseline_output = self.BENCH_ROOT / "df_closed_same_day.csv"
        assert self.baseline_input.exists(), "Baseline input file missing"
        assert self.baseline_output.exists(), "Baseline expected output missing"

        # Reset runtime sandbox under regression/tmp/runtime
        self.runtime_root = self.TMP_ROOT / "runtime"
        shutil.rmtree(self.runtime_root, ignore_errors=True)
        self.runtime_root.mkdir(parents=True, exist_ok=True)

        # Patch filesystem base dir to isolate artifacts.
        self._patchers = [
            mock.patch("hbc.utils.get_dir_base", lambda: self.runtime_root),
            mock.patch(
                "hbc.ltp.persistence.persist.ul.get_dir_base",
                lambda: self.runtime_root,
            ),
        ]
        for p in self._patchers:
            p.start()

        # Ensure app_context directories use the patched base.
        from hbc import app_context

        app_context.dir_cache = ul.get_dir_cache()
        app_context.dir_analytics = ul.get_dir_analytics()
        app_context.dir_logging = ul.get_dir_logging()
        app_context.as_of = ul.str_as_date(self.AS_OF_STR)

        # Seed cache with baseline input so job reads from cache, not network.
        cache_dir = (
            self.runtime_root
            / "CACHE"
            / self.MONIKER
            / self.AS_OF_STR
        )
        cache_dir.mkdir(parents=True, exist_ok=True)
        self.cache_path = cache_dir / f"{self.MONIKER}.csv"
        shutil.copyfile(self.baseline_input, self.cache_path)

    def tearDown(self):
        for p in self._patchers:
            p.stop()
        shutil.rmtree(self.runtime_root, ignore_errors=True)
        shutil.rmtree(self.TMP_ROOT, ignore_errors=True)

    def test_job_analysis_produces_expected_closed_same_day(self):
        job_analysis_nyc_311(
            as_of=self.AS_OF_STR, n_worst=1, n_best=1, n_days=1
        )

        produced_path = (
            self.runtime_root
            / "ANALYTICS"
            / self.MONIKER
            / ul.date_as_str(ul.str_as_date(self.AS_OF_STR))
            / "tables"
            / "df_closed_same_day.csv"
        )
        self.assertTrue(produced_path.exists(), "analysis output not created")

        produced_df = pd.read_csv(produced_path)
        expected_df = pd.read_csv(self.baseline_output)

        assert_frame_equal(produced_df, expected_df, check_like=True)
