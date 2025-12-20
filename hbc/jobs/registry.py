from hbc.jobs.pipeline.job_nyc_open_data import job_poll_nyc_open_data_311
from hbc.jobs.analytics.job_analytical_dashboard import (
    job_nyc_open_data_analyse,
)


JOB_REGISTRY = {
    "job_poll_nyc_open_data_311": job_poll_nyc_open_data_311,
    "job_nyc_open_data_analyse": job_nyc_open_data_analyse,
}
