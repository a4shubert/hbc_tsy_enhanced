from abc import ABC, abstractmethod
import logging

import pandas as pd

import hbc.ltp.fetching

logger = logging.getLogger()


class Fetcher(ABC):
    @abstractmethod
    def fetch(self, config, as_of=None) -> pd.DataFrame:
        raise NotImplementedError()

    @staticmethod
    @abstractmethod
    def validate(df: pd.DataFrame) -> pd.DataFrame:
        raise NotImplementedError()

    @staticmethod
    @abstractmethod
    def clean(df: pd.DataFrame) -> pd.DataFrame:
        raise NotImplementedError()

    @staticmethod
    @abstractmethod
    def normalize(df: pd.DataFrame) -> pd.DataFrame:
        raise NotImplementedError()

    @staticmethod
    def finalize(df: pd.DataFrame) -> pd.DataFrame:
        # why: hook for dedupe/sort/reorder after validation without changing normalize()
        return df

    def get(self, config, as_of=None) -> pd.DataFrame:
        logger.info(f"config={config}")
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
        if name == "FetcherNYCOpenData":
            return hbc.ltp.fetching.FetcherNYCOpenData()
        raise NotImplementedError(f"Fetcher {name} is not implemented")
