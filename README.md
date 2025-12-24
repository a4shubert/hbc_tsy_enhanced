# HBC: General Library for Data Pipelining & Analytics

## Structure:

Infrastructure:

- `hbc_configs`: `yaml`-based configs for DataContainers
- `hbc_db`: sqllite database

Pacakges:

- `hbc_py`: python library
- `hbc_web`: web-portal
- `hbc_rest`: REST-full api for CRUD operations

# ToDo:

- we need to remove all the rest-api related methods away from db.py into rest.py
- db.py should be ultimately depricated or just as an alternative way to look into the database
  - replaced with sql-alchemy
- ultimately creating dataclasses in asp.net from yaml configs
  - if we don't have a table with that moniker yet, we create a data class, run migration ... need to think more
