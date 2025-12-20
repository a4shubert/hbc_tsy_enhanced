from hbc.api.container import DataContainer

if __name__ == "__main__":
    dc = DataContainer("nyc_open_data_311_service_requests")
    dc.get()
