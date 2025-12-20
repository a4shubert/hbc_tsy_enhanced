import pandas as pd
from sodapy import Socrata


class Loader:
    def __init__(self, config: dict):
        self.config = config

    def load_from_config(self):
        fetcher = self.config['fetcher']



