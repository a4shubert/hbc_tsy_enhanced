import logging
from abc import ABC, abstractmethod

import pandas as pd

logger = logging.getLogger()


class Fetcher(ABC):
    """Abstract base for fetch/clean/normalize/validate pipelines."""

    MAX_TOP = 100

    @abstractmethod
    def fetch(self, config, query: str | None = None) -> pd.DataFrame:
        """Retrieve raw data for the given config/query string."""
        raise NotImplementedError()

    @classmethod
    def from_name(cls, name):
        """Factory to return a concrete Fetcher by short name."""
        if name == "FetcherNYCOpenData":
            from hbc.ltp.loading.fetchers.fetch_nycopen import FetcherNYCOpenData

            return FetcherNYCOpenData()
        raise NotImplementedError(f"Fetcher {name} is not implemented")
   
