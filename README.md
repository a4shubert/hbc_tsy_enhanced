# NYC 311 Service Requests Data Pipeline

<hr>

# Structure

## Load:

[Dataset: 311 Service Requests from 2010 to Present](https://data.cityofnewyork.us/Social-Services/311-Service-Requests-from-2010-to-Present/erm2-nwe9/about_data)

[Alternative dataset: 311 Service Requests for 2009](https://data.cityofnewyork.us/Social-Services/311-Service-Requests-for-2009/3rfa-3xsf/about_data)

App token setup: https://data.cityofnewyork.us/profile/edit/developer_settings

Each column in the dataset is represented by a single field in its SODA API. Using SoQL queries, you can search for records, limit your results, and change the way the data is output.

Help on method get in module sodapy.socrata:

```get(dataset_identifier, content_type='json', **kwargs) method of sodapy.socrata.Socrata instance
Read data from the requested resource. Options for content_type are json,
csv, and xml. Optionally, specify a keyword arg to filter results:

        select : the set of columns to be returned, defaults to *
        where : filters the rows to be returned, defaults to limit
        order : specifies the order of results
        group : column to group results on
        limit : max number of results to return, defaults to 1000
        offset : offset, used for paging. Defaults to 0
        q : performs a full text search for a value
        query : full SoQL query string, all as one parameter
        exclude_system_fields : defaults to true. If set to false, the
            response will include system fields (:id, :created_at, and
            :updated_at)

    More information about the SoQL parameters can be found at the official
    docs:
        http://dev.socrata.com/docs/queries.html

    More information about system fields can be found here:
        http://dev.socrata.com/docs/system-fields.html
```

#### For data-pipeline structure:

- we implement `yaml` based configs and for now do not validate the completeness of the keys, we take [] operator assuming all the keys are present in the config
- `fetch` method on Fetcher will return pandas DataFrame just as a short-cut for the time being, subsequently more nuanced data structures can be implemented
- we organize CACHE for now as simply .csv files each of which is persisted in a designated folder parametrized by `as_of` date

**For an incremental retrieval:**

we notice the ordering of `unique_key` does not correspond to the ordering of `created_date`, thus

- at a daily intervals (perhaps for as_of=T we download T-1 data):
  - we download the `created_date` = T-1

**For an consistency retrieval:**

- at less frequent intervals we restore consistency to CACHE by
  - obtaining all the unique `created_dates` in dataset
  - comparing with the dates available in CACHE
  - retrieving all the missing dates

## Transform:

## Persist:

## Quant:
