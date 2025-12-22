from hbc.jobs.job_pipeline import job_fetch_nyc_open_data_311_service_requests
from hbc.jobs.job_analytics import job_analyse_nyc_open_data_311_service_requests


JOB_REGISTRY = {
    "job_fetch_nyc_open_data_311_service_requests": job_fetch_nyc_open_data_311_service_requests,
    "job_analyse_nyc_open_data_311_service_requests": job_analyse_nyc_open_data_311_service_requests,
}
