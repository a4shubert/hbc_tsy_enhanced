# NYC 311 Service Requests Data Pipeline

Library + jobs to fetch, cache, and analyze NYC 311 service request data.

_Updates_:

- DataContainer `df` setter now validates columns against the config schema on assignment.
- `app_context` is simplified; set the logical date directly (e.g., `app_context.as_of = datetime.date(...)`).
- Fetchers always receive a non-`None` `as_of` from DataContainer.
- Cache snapshots are stored as gzipped CSVs; `from_cache` transparently ungzips and re-zips.
- Job dispatch adds `--dir-base`, `--dir-cache`, `--dir-analytics`, and `--dir-logging` to override runtime dirs; utility paths stay aligned when `--dir-base` is provided.

## Install

```bash
pip install git+https://github.com/a4shubert/hbc_tsy.git
# or, from a checkout:
# pip install .
```

## Running as Jobs

Use the job dispatcher to execute the built-in pipelines. Artifacts (cache/logs/analytics) are written under `app_context.dir_base` (see `utils.get_dir_base` for the current location; override as needed). The installed package name is `hbc` (repo: `hbc_tsy`).

```bash
# Poll one day of data into cache
python -m hbc.jobs.dispatch --job-name=job_poll_nyc_311 --as-of=2009-12-31 --incremental=True --log-level=INFO

# Run analytics for that date
python -m hbc.jobs.dispatch --job-name=job_analysis_nyc_311 --as-of=2009-12-31 --n-worst=10 --n-best=10 --n-days=10 --log-level=INFO

# Restore cache integrity for the last few missing dates (fetches multiple days)
python -m hbc.jobs.dispatch --job-name=job_poll_nyc_311 --as-of=2009-12-31 --incremental=False --last-missing-dates=5 --log-level=INFO
```

### Midnight Scheduler (optional)

Run both jobs every midnight:

```bash
python -m hbc.jobs.runner
```

## Using as a Library

Fetch data programmatically, save to cache, and read it back.

```python
from hbc import DataContainer, app_context, utils as ul

# set logical as-of date (defaults to today)
app_context.as_of = "2009-12-31"

# Fetch once and cache
dc = DataContainer("nyc_open_data_311_service_requests")
dc.get(app_context.as_of)
dc.to_cache(app_context.as_of)

# Later: load from cache
df = dc.from_cache(app_context.as_of, retrieve_if_missing=False)
print(df.head())
```

## Demo Notebook

A walk-through lives in `notebooks/Demo.html` (rendered) and the accompanying notebook. Open it in your browser or notebook viewer to see end-to-end examples of fetching, cleaning, and plotting the data.
