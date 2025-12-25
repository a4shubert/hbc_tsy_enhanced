import shutil
import unittest
from pathlib import Path
from unittest import mock

import pandas as pd
from pandas.testing import assert_frame_equal

from hbc.jobs.job_pipeline import job_fetch_nyc_open_data_311_service_requests
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
        ]

        for p in self._patchers:
            p.start()

        # Ensure app_context directories use the patched base.
        from hbc import app_context

        app_context.dir_base = self.runtime_root
        app_context.dir_analytics = ul.mk_dir(
            app_context.dir_base / "ANALYTICS"
        )
        app_context.dir_logging = ul.mk_dir(app_context.dir_base / "LOGS")

        # Patch fetch to avoid network and return deterministic data.
        baseline_df = pd.read_csv(self.baseline_path)
        self.fetch_patcher = mock.patch(
            "hbc.ltp.loading.fetchers.fetch_nycopen.FetcherNYCOpenData.fetch",
            return_value=baseline_df,
        )
        self.fetch_patcher.start()

        # Patch REST post to capture payload instead of real HTTP.
        self.rest_post_calls = []
        def _fake_post(table, df, verify=None):
            self.rest_post_calls.append((table, df.copy(), verify))
            return [200] * len(df)
        self.rest_post_patcher = mock.patch(
            "hbc.ltp.persistence.rest.RestApi.post",
            side_effect=_fake_post,
        )
        self.rest_post_patcher.start()

    def tearDown(self):
        self.fetch_patcher.stop()
        self.rest_post_patcher.stop()
        for p in self._patchers:
            p.stop()
        shutil.rmtree(self.runtime_root, ignore_errors=True)
        shutil.rmtree(self.TMP_ROOT, ignore_errors=True)

    def test_job_poll_creates_expected_cache(self):
        job_fetch_nyc_open_data_311_service_requests(as_of=self.AS_OF_STR, incremental=True)
        self.assertTrue(self.rest_post_calls, "REST cache was not called")
        table, df_posted, _ = self.rest_post_calls[0]
        self.assertEqual(table, self.MONIKER)
        expected_df = pd.read_csv(self.baseline_path)
        assert_frame_equal(df_posted.reset_index(drop=True), expected_df, check_like=True)
