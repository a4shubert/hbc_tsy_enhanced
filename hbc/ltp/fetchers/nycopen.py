import pandas as pd
from sodapy import Socrata

from hbc.ltp.fetchers import Fetcher


class FetcherNYCOpenData(Fetcher):

    @staticmethod
    def validate(df) -> pd.DataFrame:
        return df

    @staticmethod
    def clean(df) -> pd.DataFrame:
        return df

    @staticmethod
    def normalize(df) -> pd.DataFrame:
        return df

    @classmethod
    def fetch(cls, config) -> pd.DataFrame:
        token = config['token']
        base_url = config['base_url']
        sub_url = config['url']
        query_kwargs = config['kwargs']
        query_kwargs = {k: v for k, v in query_kwargs.items() if v is not None}
        print(query_kwargs)
        client = Socrata(base_url, token, None)
        res = client.get(
            sub_url,
            **query_kwargs
        )
        return pd.DataFrame.from_records(res)

