from hbc.jobs.pipeline.job_data import job_poll_nyc_open_data_311
from hbc.jobs.analytics.job_analytics import (
    job_nyc_open_data_analysis,
)


JOB_REGISTRY = {
    "job_poll_nyc_open_data_311": job_poll_nyc_open_data_311,
    "job_nyc_open_data_analyse": job_nyc_open_data_analysis,
}
