from hbc.abstract.container import DataContainer

if __name__ == "__main__":
    dc = DataContainer("fetcher_nyc_open_data")
    dc.get()
