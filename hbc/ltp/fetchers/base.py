from abc import ABC, abstractmethod

import pandas as pd


class Fetcher(ABC):

    @abstractmethod
    def fetch(self, config) -> pd.DataFrame:
        raise NotImplementedError()

    @staticmethod
    @abstractmethod
    def validate(df) -> pd.DataFrame:
        raise NotImplementedError()

    @staticmethod
    @abstractmethod
    def clean(df) -> pd.DataFrame:
        raise NotImplementedError()

    @staticmethod
    @abstractmethod
    def normalize(df) -> pd.DataFrame:
        raise NotImplementedError()

    def get(self, config) -> pd.DataFrame:
        df = self.fetch(config)
        df = self.validate(df)
        df = self.clean(df)
        df = self.normalize(df)
        return df
