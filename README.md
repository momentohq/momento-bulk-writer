# rdb-to-momento

ETL for users with a Redis database in hand

# Project Structure

```
.
├── data ............................ sample data
│   └── redis ....................... path to write redis snapshots
├── scripts.......................... scripts run all or part of the pipeline
├── src .............................
│   └── Momento.Etl .................
│       ├── RedisLoadGenerator ...... redis load gen to create rdb files
|       ├── Model ................... redis data model basd on redis-rdb-cli jsonl
|       ├── Validation .............. momento-redis data validation
|       └── Cli ..................... cli to run validator and loader
├── RdbToMomento.sln ................ repo solution file
├── LICENSE ......................... apache 2.0 license
├── Makefile ........................ makefile to build, clean, publish, and dist
└── README.md ....................... hey that's me

```

# How to Build, Test, and Distribute

- Run `make help` to see make options.

- Run `make build` to build.

- Run `make test` to run unit tests.

- Run `make dist` to build standalone executables and package scripts.
  - NB: this builds executables for linux, windows, and macos

# How to Run (harder - by hand)

## Obtain an RDB file (Redis database)

1. Create a backup in Elasticache, or

2. Run `BGSAVE` on an existing Redis instance, or

3. Use the `RedisLoadGenerator` project to generate a Redis database. See the project README for details.

## Extract RDB to JSONL

Use the `redisrdbcli/redis-rdb-cli` docker image to convert the Redis database (as RDB) to JSON lines. See the script `scripts/rdb-to-jsonl.sh` for an example invocation. This script mounts a host directory in the docker container so the container may read the Redis database and write the JSON lines.

The script assumes a directory structure where the rdb file and eventual output share a common ancestor, eg

```
├── data ............................ mount point on host
|   ├── redis ....................... path to write redis snapshots
|   |   └── snapshot.rdb............. redis snapshot
|   └── stage1
|       └── snapshot.jsonl .......... redis snapshot as json lines
```

To generate `snapshot.jsonl` from `snapshot.rdb`, run the script with `rdb-to-json.sh ./data redis/snapshot.rdb stage1/snapshot.jsonl`

## Validate the database

i. The validate tool identifies potential incompatibilities with Momento. When running the first time, we should explore all potential incompatiblities. To do this, enable all the flags:

`./MomentoEtl validate --maxTtl <MAX-TTL-IN-DAYS> --maxPayloadSize <MAX-SIZE-IN-MiB-OF-ITEM> --filterLongTtl --filterAlreadyExpired --filterMissingTtl <DATA-PATH> <VALID-PATH> <ERROR-PATH>`

For example:

`./MomentoEtl validate --maxTtl 1 --maxPayloadSize 1 --filterLongTtl --filterAlreadyExpired --filterMissingTtl snapshot.jsonl valid.jsonl error.jsonl`

Which reads from `snapshot.jsonl`, writes data that passes the filters to `valid.jsonl` and those that do not to `error.jsonl`. `error.jsonl` contains two columns separated by a tab. The first column contains an error message. This allows you to easily grep error types for analysis. Specifically an end user may wish to know which items have a longer TTL than allowed, which items are too big, and which data types are not supported.

ii. After doing an initial analysis, run the tool with a relaxed set of filters. Because a TTL that is too long can be clipped, a missing one can have one applied, etc. we can still store these items. Because an item that is too large is not recoverable and neither is an unsupported data type, we still filter those:

`./MomentoEtl validate --maxPayloadSize 1 snapshot.jsonl valid.jsonl error.jsonl`

## Load the data into Momento

To store the data in Momento, run this command:

`./MomentoEtl load -a <AUTH-TOKEN> -c <CACHE-NAME> --defaultTtl <DEFAULT-TTL> --maxTtl <MAX-TTL> <DATA-PATH>`

Asssuming we created a `valid.jsonl` file from step 3, we would run:

`./MomentoEtl load -a <AUTH-TOKEN> -c <CACHE-NAME> --defaultTtl 1 --maxTtl 1 valid.jsonl`

Note:

- We assume that the cache has already been created. We recommend this since, because loading the data all at once demands much rate and throughput, the cache limits ought to be adjusted beforehand. If we create the caache as part of the load command, then limits will be default and too low.

- The `maxTtl` option is used to clip excessive TTLs. Eg if a cache has a limit of 1 day TTL and an item has two days left to expire, we will load it with a TTL of 1 day. Similarly if an item lacks a TTL, we will apply `defaultTtl` in days.

- By default we discard items that have already expired.
  - There is an option to not do this; see the CLI for details. (Why would we want to override this? Suppose we are testing how long it takes to load a snapshot. As the snapshot ages, more and more items will expire. Eventually it becomes useless. Hence to test how long it takes to load a particular snapshot, we want to load expired items. This way at least we estimate the worst case.)

# How to Run (easier - using scripts)

## Build a deployable package by running:

`make dist`

This produces `dist/momento-etl.tgz` with the scripts and binaries bundled together.

## Extract and validate

The script `extract-and-validate.sh` wraps the extract and validate steps above. We assume the same directory structure as above with a parent directory `data` and subdirectory `data/redis` that contains the rdb files. Example:

`./extract-and-validate.sh path-to-data-dir linux-x64/MomentoEtl 1 1`

## Load

Load the data into Momento. Use `load-one.sh` to load a single file serially. Use `load-many.sh` to split the file into chunks and load in parallel. Example:

`./load-one.sh path-to-validated-file linux-x64/MomentoEtl auth-token cache-name 1 1`

where `path-to-validated-file` would be produced by extract and validated, eg in `data/stage3-lax/valid`.
