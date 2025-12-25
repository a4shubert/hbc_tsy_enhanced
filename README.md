# HBC_TSY_ENCHANCED

## Data LTP & Analytical Dashboard

Rich-features library for data retrieval, transformations, persistance, analytics and web-presentation.

## Structure:

Infrastructure:

- `hbc_configs`: `yaml`-based configs for DataContainers
- `hbc_db`: sqllite database

Pacakges:

- `hbc_py`: python library
  - `ltp`:
    - a uniform query language OData for both fetching and caching
- `hbc_web`: web-portal
- `hbc_rest`: REST-full api for CRUD operations

# HowTo:

## `ltp`:

- onboard new fetcher
- retrieve data using OData query language
- cache data
- retrieve data from cache using OData query language

# ToDo:

- [x] we need to remove all the rest-api related methods away from db.py into rest.py

- [x] db.py should be ultimately depricated or just as an alternative way to look into the database
  - [ ] replaced with sql-alchemy
- [ ] ultimately creating dataclasses in asp.net from yaml configs
