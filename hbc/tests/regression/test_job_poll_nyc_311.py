import shutil
import unittest
from pathlib import Path
from unittest import mock

import pandas as pd
from pandas.testing import assert_frame_equal

from hbc.jobs.job_pipeline import job_poll_nyc_311
import hbc.utils as ul


class TestJobPollNYC311(unittest.TestCase):
    TMP_ROOT = Path(__file__).resolve().parent / "tmp"
    BENCH_ROOT = Path(__file__).resolve().parent / "benchmark"
    MONIKER = "nyc_open_data_311_service_requests"
    AS_OF_STR = "20091231"

    def setUp(self):
        self.TMP_ROOT.mkdir(parents=True, exist_ok=True)
        # Baseline artifact lives in regression/benchmark.
        self.baseline_path = self.BENCH_ROOT / f"{self.MONIKER}.csv"
        assert self.baseline_path.exists(), "Baseline regression file missing"

        shutil.rmtree(self.TMP_ROOT / "runtime", ignore_errors=True)
        self.runtime_root = self.TMP_ROOT / "runtime"
        self.runtime_root.mkdir(parents=True, exist_ok=True)

        # Patch filesystem base dir to isolate artifacts.
        self._patchers = [
            mock.patch("hbc.utils.get_dir_base", lambda: self.runtime_root),
            mock.patch(
                "hbc.ltp.persistence.cache.ul.get_dir_base",
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

        # Patch fetch to avoid network and return deterministic data.
        baseline_df = pd.read_csv(self.baseline_path)
        self.fetch_patcher = mock.patch(
            "hbc.ltp.loading.fetchers.nycopen.FetcherNYCOpenData.fetch",
            return_value=baseline_df,
        )
        self.fetch_patcher.start()

    def tearDown(self):
        self.fetch_patcher.stop()
        for p in self._patchers:
            p.stop()
        shutil.rmtree(self.runtime_root, ignore_errors=True)
        shutil.rmtree(self.TMP_ROOT, ignore_errors=True)

    def test_job_poll_creates_expected_cache(self):
        job_poll_nyc_311(as_of=self.AS_OF_STR, incremental=True)

        produced_path = (
            self.runtime_root
            / "CACHE"
            / self.MONIKER
            / self.AS_OF_STR
            / f"{self.MONIKER}.csv"
        )
        self.assertTrue(produced_path.exists(), "cache file was not created")

        produced_df = pd.read_csv(produced_path)
        expected_df = pd.read_csv(self.baseline_path)

        assert_frame_equal(produced_df, expected_df, check_like=True)
