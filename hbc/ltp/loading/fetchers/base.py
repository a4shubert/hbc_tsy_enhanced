import logging
from abc import ABC, abstractmethod

import pandas as pd

logger = logging.getLogger()


class Fetcher(ABC):
    """Abstract base for fetch/clean/normalize/validate pipelines."""

    @abstractmethod
    def fetch(self, config, **query_kwargs) -> pd.DataFrame:
        """Retrieve raw data for the given config/query kwargs."""
        raise NotImplementedError()

    @classmethod
    def from_name(cls, name):
        """Factory to return a concrete Fetcher by short name."""
        if name == "FetcherNYCOpenData":
            from hbc.ltp.loading.fetchers.nycopen import FetcherNYCOpenData

            return FetcherNYCOpenData()
        raise NotImplementedError(f"Fetcher {name} is not implemented")

    @property
    def validator_name(self) -> str:
        """Return the default validator name for this fetcher."""
        return "ValidatorGeneric"

    def get(self, config, **query_kwargs) -> pd.DataFrame:
        """Full pipeline: fetch -> validator.parse (clean/normalize/validate/finalize)."""
        from hbc.ltp.loading.validators import Validator

        logger.info("loading...")
        df = self.fetch(config, **query_kwargs)
        if len(df):
            validator = Validator.from_name(
                config.get("validator", self.validator_name)
            )
            logger.info("parsing (clean/normalize/validate/finalize)...")
            df = validator.parse(df)
        return df
