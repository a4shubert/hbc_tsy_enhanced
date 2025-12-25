import datetime
import shutil
import unittest
from pathlib import Path


import hbc.utils as ul


class TestUtils(unittest.TestCase):
    TMP_ROOT = Path(__file__).resolve().parent / "tmp"

    def setUp(self):
        shutil.rmtree(self.TMP_ROOT, ignore_errors=True)
        self.TMP_ROOT.mkdir(parents=True, exist_ok=True)

    def tearDown(self):
        shutil.rmtree(self.TMP_ROOT, ignore_errors=True)

    def test_str_as_date_accepts_date_and_string(self):
        today = datetime.date(2024, 1, 2)
        self.assertEqual(ul.str_as_date(today), today)
        self.assertEqual(ul.str_as_date("2024-01-02"), today)

    def test_jsonify_unhashable_makes_stable_strings(self):
        payload = {"b": 1, "a": [3, 2]}
        serialized = ul._jsonify_unhashable(payload)
        self.assertEqual(serialized, '{"a": [3, 2], "b": 1}')

    def test_sheetify_sanitizes_invalid_chars(self):
        name = "Bad:Name/With*Chars?"
        self.assertEqual(ul._sheetify(name), "Bad_Name_With_Chars_")

    def test_paths_to_str_handles_pathlike_and_str(self):
        with self.subTest("pathlike and str handled"):
            p1 = self.TMP_ROOT / "file.txt"
            p1.touch()
            p2 = "another.txt"
            self.assertEqual(ul.paths_to_str([p1, Path(p2)]), [str(p1), p2])
