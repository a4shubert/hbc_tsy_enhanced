# HBC_TSY_ENHANCED

Hybrid data pipeline that pulls NYC 311 datasets from Socrata, validates/normalizes them in Python, and persists them through a minimal ASP\.NET EF Core REST API backed by SQLite.

# Table of Contents

- [Package Map](#package-map)
- [Installation](#installation)
- [Usage Examples](#usage-examples)
  - [Library](#library)
  - [Jobs](#jobs)
- [Components](#components)
  - [hbc_configs (yaml)](#hbc_configs-yaml)
  - [hbc_db (sqlite)](#hbc_db-sqlite)
  - [hbc_py (Python)](#hbc_py-python)
    - [UML (High-Level)](#uml-high-level)
  - [hbc_rest (ASPNet EF Core)](#hbc_rest-aspnet-ef-core)
  - [hbc_web (Next.js)](#hbc_web-nextjs)

# Package Map

- `hbc_configs`: Source configs (YAML) defining schemas, types, and source tokens for each moniker.
- `hbc_db`: SQLite database location (`hbc.db` lives here by default).
- `hbc_py`: Python package with DataContainer/fetchers/validators, jobs, and tests.
- `hbc_rest`: ASP\.NET Core 8 minimal API + EF Core 8 exposing CRUD/batch endpoints over the same schemas.
- `hbc_web`: Placeholder for a web/UI surface (no active code yet).

- `scripts`: Shared helpers (`env.sh` for env vars) and REST build/run scripts under `hbc_rest/scripts`.

# Installation

**Prerequisites**: Python 3.10+, .NET 8 SDK, and optionally Miniconda/conda if you prefer conda-based environments.

1.  Clone and enter the repo:
    ```bash
    git clone https://github.com/a4shubert/hbc_tsy_enhanced.git
    cd hbc_tsy_enhanced
    ```
2.  macOS/Linux:

    - One-shot setup (env, venv, hbc_py install, start API):

      ```bash
      ./install.sh
      ```

      - Python only: `pip install -e hbc_py`
      - API only: `hbc_rest/scripts/run_prod.sh` (Swagger UI at `$ASPNETCORE_URLS/swagger/index.html`)
        <br>

    - Demo notebook (classic):

      ```bash
      hbc_py/scripts/run_demo_notebook.sh
      ```

3.  Windows (PowerShell):

    - One-shot setup (env, venv, hbc_py install, start API):

      ```bash
      .\install.ps1
      ```

      - Python only: `pip install -e hbc_py`
      - API only: `hbc_rest\scripts\run_prod.ps1` (Swagger UI at `$ASPNETCORE_URLS/swagger/index.html`)
        <br>

    - Demo notebook (classic):

      ```bash
      hbc_py\scripts\run_demo_notebook.ps1
      ```

# Usage Examples

## Library

_Fetch 311 service requests from Socrata_:

```python
from hbc import DataContainer
dc = DataContainer("nyc_open_data_311_service_requests")
dc.get(query="$filter=created_date ge '2010-01-01' and agency eq 'NYPD'&$top=100")
dc.to_cache()  # persists via REST into SQLite
```

_Read back cached rows_:

```python
dc.from_cache(query="$top=10")
print(dc.df.head())
```

## Jobs:

Use the job dispatcher to execute the built-in pipelines.

_Poll one day of data into cache_:

```bash
python -m hbc.jobs.dispatch --job-name=job_fetch_nyc_open_data_311_service_requests --as-of=2009-12-31 --incremental=True --log-level=INFO
```

_Run analytics for that date_:

```bash
python -m hbc.jobs.dispatch --job-name=job_analyse_nyc_open_data_311_service_requests --as-of=2009-12-31 --n-worst=10 --n-best=10 --n-days=10 --log-level=INFO
```

_Restore cache integrity for the last few missing dates (fetches multiple days)_:

```bash

python -m hbc.jobs.dispatch --job-name=job_fetch_nyc_open_data_311_service_requests --as-of=2009-12-31 --incremental=False --last-missing-dates=5 --log-level=INFO
```

_Midnight Scheduler_:

```bash
python -m hbc.jobs.runner
```

# Components

## hbc_configs (yaml)

- YAML files defining schemas and metadata for each moniker (NYC Open Data datasets).
- Tokens/IDs are read from here by both Python fetchers and the REST API models.

## hbc_db (sqlite)

- Default SQLite database location. `reset_db.sh` recreates it with the latest migrations.

## hbc_py (Python)

### api:

- **Context** (`hbc/api/context`): carries logical date and dirs; CLI dispatch can override dirs/date, and utility helpers honor an overridden base dir for consistent artifact locations.
- **DataContainer** (`hbc/api/container`): entry point for each moniker (`dc = DataContainer("nyc_open_data_311_service_requests")`).
  - Fetch upstream via Socrata-like query strings: `dc.get(query="$filter=agency eq 'NYPD'&$top=250")`
  - Cache to REST API/SQLite: `dc.to_cache()`
  - Read from cache: `dc.from_cache(query="$top=10")`
  - Schema enforcement: missing columns are added as `None`; `hbc_unique_key` is auto-generated and retained end-to-end.

### ltp:

- **Fetchers** (`hbc/ltp/loading/fetchers`): fetch only `FetcherNYCOpenData` wraps Socrata with retries/backoff, pagination, etc. Fetcher factory resolves by name from config.
- **Validators** (`hbc/ltp/loading/validators`): clean/normalize/validate/finalize via `Validator.parse`. Default is `ValidatorGeneric` (no-op); `ValidatorNYCOpen311Service` implements NYC-specific rules and logging.

### jobs:

- **Jobs**: (`hbc_py/hbc/jobs`) with dispatch tooling for CLI runs.

### quant:

- **Analytics/Plots** (`hbc/quant/analysis.py`, `hbc/quant/plots.py`): `AnalyticalEngine` provides ranking/summary helpers (best/worst/mean/median); `PlotEngine` offers plotting utilities for time series, bars, and geo bubbles.

### tests:

- Lint: `ruff check hbc_py/hbc`
- Unit tests: (`pytest hbc_py/hbc/tests/unit`)
- Integration (live Socrata + REST): `HBC_INTEGRATION=1 pytest hbc_py/hbc/tests/integration` (requires running REST API and valid tokens in `.env`).

#### UML (High-Level)

##### Library

```mermaid
classDiagram
    class DataContainer {
      -config
      -moniker
      -df
      +get(query)
      +to_cache()
      +from_cache(query)
    }
    class Fetcher {
      <<abstract>>
      +fetch(config, query)
    }
    class FetcherNYCOpenData {
      +fetch(config, query)
    }
    class Validator {
      <<abstract>>
      +parse(df)
      +clean(df)
      +normalize(df)
      +validate(df)
      +finalize(df)
    }
    class ValidatorGeneric {
      +parse(df)
    }
    class ValidatorNYCOpen311Service {
      +parse(df)
    }
    class RestApi {
      +get(table, query)
      +post(table, df)
    }
    DataContainer --> Fetcher : selects by name
    Fetcher <|-- FetcherNYCOpenData
    DataContainer --> Validator : selects by name
    Validator <|-- ValidatorGeneric
    Validator <|-- ValidatorNYCOpen311Service
    DataContainer --> RestApi : persist/load df
    class AnalyticalEngine {
      +top_n_best(...)
      +top_n_worst(...)
      +mean(...)
      +median(...)
      +descriptive_stats(...)
    }
    class PlotEngine {
      +time_series(...)
      +bar(...)
      +geo_bubble(...)
    }
    RestApi ..> AnalyticalEngine : supplies cached data
    RestApi ..> PlotEngine : supplies cached data
```

##### Jobs

```mermaid
classDiagram
    class Dispatcher {
      +main(argv)
    }
    class Registry {
      +JOB_REGISTRY
    }
    class Runner {
      +midnight_scheduler(...)
    }
    class job_fetch_nyc_open_data_311_service_requests {
      +job_fetch_nyc_open_data_311_service_requests(as_of, incremental, last_missing_dates)
    }
    class job_analyse_nyc_open_data_311_service_requests {
      +job_analyse_nyc_open_data_311_service_requests(as_of, n_worst, n_best, n_days)
    }
    Dispatcher --> Registry : uses JOB_REGISTRY
    Registry ..> job_fetch_nyc_open_data_311_service_requests
    Registry ..> job_analyse_nyc_open_data_311_service_requests
    Runner ..> Dispatcher : schedules commands
```

## hbc_rest (ASP\.Net EF Core)

- Minimal API (net8.0) with EF Core + SQLite.
- Endpoints per moniker:
  - `GET /{table}?` supports `$filter`, `$select`, `$orderby`, `$top`, `$skip`, `$count`, `$expand`
  - `POST /{table}/batch` for inserts/upserts (expects `hbc_unique_key`)
  - `DELETE /{table}/{hbc_unique_key}` for cleanup
- Logging middleware traces every request; DELETE is available for test data cleanup.
- Environment vars: `HBC_DB_PATH` (SQLite file), `HBC_API_URL` & `ASPNETCORE_URLS` (listener), `ASPNETCORE_ENVIRONMENT` (Dev/Production).

## hbc_web (Next.js)

- Reserved for future UI; currently empty.
