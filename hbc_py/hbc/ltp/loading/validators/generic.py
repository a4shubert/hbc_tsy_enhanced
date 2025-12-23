import pandas as pd

from hbc.ltp.loading.validators.base import Validator


class ValidatorGeneric(Validator):
    """No-op validator; returns the DataFrame unchanged."""

    @staticmethod
    def validate(df: pd.DataFrame) -> pd.DataFrame:
        return df

    @staticmethod
    def clean(df: pd.DataFrame) -> pd.DataFrame:
        return df

    @staticmethod
    def normalize(df: pd.DataFrame) -> pd.DataFrame:
        return df
