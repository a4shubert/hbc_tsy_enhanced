from abc import ABC, abstractmethod
import pandas as pd
import hbc.ltp.fetching


class Fetcher(ABC):
    @abstractmethod
    def fetch(self, config) -> pd.DataFrame:
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

    def get(self, config) -> pd.DataFrame:
        print(f"config={config}")
        print("fetching...")
        df = self.fetch(config)
        if len(df):
            print("cleaning...")
            df = self.clean(df)

            print("normalizing...")
            df = self.normalize(df)

            print("validating...")
            df = self.validate(df)

            print("finalizing...")
            df = self.finalize(df)
        return df

    @classmethod
    def from_name(cls, name):
        if name == "FetcherNYCOpenData":
            return hbc.ltp.fetching.FetcherNYCOpenData()
        raise NotImplementedError(f"Fetcher {name} is not implemented")
