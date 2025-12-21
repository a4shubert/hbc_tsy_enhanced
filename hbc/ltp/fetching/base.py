from abc import ABC, abstractmethod
import logging

import pandas as pd

import hbc.ltp.fetching

logger = logging.getLogger()


class Fetcher(ABC):
    """Abstract base for fetch/clean/normalize/validate pipelines."""

    @abstractmethod
    def fetch(self, config, as_of=None) -> pd.DataFrame:
        """Retrieve raw data for the given config/date."""
        raise NotImplementedError()

    @staticmethod
    @abstractmethod
    def validate(df: pd.DataFrame) -> pd.DataFrame:
        """Validate dataset; may annotate or raise."""
        raise NotImplementedError()

    @staticmethod
    @abstractmethod
    def clean(df: pd.DataFrame) -> pd.DataFrame:
        """Clean/standardize raw data (strip/trim, etc.)."""
        raise NotImplementedError()

    @staticmethod
    @abstractmethod
    def normalize(df: pd.DataFrame) -> pd.DataFrame:
        """Normalize schema/types before validation."""
        raise NotImplementedError()

    @staticmethod
    def finalize(df: pd.DataFrame) -> pd.DataFrame:
        # why: hook for dedupe/sort/reorder after validation without changing normalize()
        """Optional final tweaks after validation; defaults to passthrough."""
        return df

    def get(self, config, as_of=None) -> pd.DataFrame:
        """Full pipeline: fetch -> clean -> normalize -> validate -> finalize."""
        logger.info("fetching...")
        df = self.fetch(config, as_of)
        if len(df):
            logger.info("cleaning...")
            df = self.clean(df)

            logger.info("normalizing...")
            df = self.normalize(df)

            logger.info("validating...")
            df = self.validate(df)

            logger.info("finalizing...")
            df = self.finalize(df)
        return df

    @classmethod
    def from_name(cls, name):
        """Factory to return a concrete Fetcher by short name."""
        if name == "FetcherNYCOpenData":
            return hbc.ltp.fetching.FetcherNYCOpenData()
        raise NotImplementedError(f"Fetcher {name} is not implemented")
