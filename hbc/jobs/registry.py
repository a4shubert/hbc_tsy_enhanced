from hbc.jobs.job_pipeline import job_poll_nyc_311
from hbc.jobs.job_analytics import (
    job_analysis_nyc_311,
)


JOB_REGISTRY = {
    "job_poll_nyc_311": job_poll_nyc_311,
    "job_analysis_nyc_311": job_analysis_nyc_311,
}
