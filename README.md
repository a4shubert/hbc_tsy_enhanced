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

- we impelent `yaml` based configs and for now do not validate the completeness of the keys, we take [] operator assuming all the keys are present in the config
- `fetch` method on Fetcher will return pandas DataFrame just as a short-cut for the time being, subsequently more nuanced data structures can be implemented

#### For an incremental retrieving:

we notice the ordering of `unique_key` does not correspond to the ordering of `created_date`, thus,

- at a regular intervals we
  - given the latest persisted `unique_key` we find it's record in the dataset and take it's `created_date`
  - we find all the unique keys with the `created_date` greater than found
  - we retrieve all the data for the found unique keys
- at a less frequent intervals we
  - get all the unique keys in the dataset (e.g. for the last 2 years)
  - identify if any are missing in our cache
  - retrieve the missing records if any

## Transform:

## Persist:

## Quant:
