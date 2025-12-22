import logging
from abc import ABC, abstractmethod

import pandas as pd

logger = logging.getLogger()


class Validator(ABC):
    """Abstract base for dataset validation/clean/normalize/finalize."""

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

    @classmethod
    def from_name(cls, name: str) -> "Validator":
        if not name or name == "ValidatorGeneric":
            from hbc.ltp.loading.validators.generic import ValidatorGeneric

            return ValidatorGeneric()
        if name == "ValidatorNYCOpen311Service":
            from hbc.ltp.loading.validators.nycopen import (
                ValidatorNYCOpen311Service,
            )

            return ValidatorNYCOpen311Service()
        raise NotImplementedError(f"Validator {name} is not implemented")

    def parse(self, df: pd.DataFrame) -> pd.DataFrame:
        """Run clean -> normalize -> validate -> finalize with logging."""
        logger.info("using validator: %s", self.__class__.__name__)
        logger.info("cleaning...")
        df = self.clean(df)

        logger.info("normalizing...")
        df = self.normalize(df)

        logger.info("validating...")
        df = self.validate(df)

        logger.info("finalizing...")
        df = self.finalize(df)

        return df
