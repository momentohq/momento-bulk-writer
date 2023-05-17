# momento-bulk-writer

This project contains a set of tools to bulk load data into Momento. It is intended to be used by Momento users who wish to bulk load from an existing data source to Momento.

Included in the project are tools to extract, validate, and load data into Momento. The tools are designed to be run in a pipeline, but can also be run individually.
Currently we have implemented a Redis to Momento pipeline, but the tools are designed to be extensible to other data sources. Popular data sources include Redis, Memcached, csv, json, parquet, and others.

If there is a data source you would like to see supported, please open an issue or submit a pull request.

# Setup

## Prerequisites

For reading a Redis database, you will need Java 8 or higher.

For running on Windows, you will either need to install bash, or run the linux version in the Windows Subsystem for Linux (WSL).

## Installation

1. Download the latest release from the [releases page]()
2. Choose between linux, osx, and windows runtimes
3. Untar and decompress the release to a directory of your choice

Example for linux:

```bash
$ wget https://github.com/momentohq/momento-bulk-writer/releases/download/${version}/momento-bulk-writer-linux-x86.tgz
$ tar xzvf momento-bulk-writer-linux-x86.tgz
$ cd ./momento-bulk-writer-linux-x86
$ ./extract-rdb.sh -h
$ ./validate.sh -h
$ ./load.sh -h
```

# Usage

We demonstrate the usage of the tools with a Redis to Momento pipeline. The pipeline consists of three steps:

1. Extract a Redis database to JSON lines
2. Validate the JSON lines
3. Load the JSON lines into Momento

## RDB files

First we need to obtain an RDB file(s). There are two ways to do this:

1. Create [a backup in Elasticache](https://docs.aws.amazon.com/AmazonElastiCache/latest/red-ug/backups-manual.html), or
2. Run [`BGSAVE`](https://redis.io/commands/bgsave/) on an existing Redis instance.

## Extract a Redis database to JSON lines and validate

### Run the tools

Let us assume we have a directory of rdb files located in `./redis` and we wish to write the output to the current directory `.`. We can do this with the `extract-rdb-and-validate.sh` script:

```bash
$ ./extract-rdb-and-validate.sh -s 1 -t 1 ./redis .
```

This will extract the rdb files in `./redis` to JSON lines and write the output to the current directory. The `-s` and `-t` flags are optional and set the max size in MiB and max ttl in days of items in the cache. If an item is larger than the max size or has a ttl longer than the max ttl, it will be flagged by the tools. The default values are 1 MiB and 1 day respectively.

Contact Momento on [Discord](https://discord.com/invite/3HkAKjUZGq) or e-mail us at [support@momentohq.com](mailto:support@momentohq.com) if you need to increase these values.

### Inspect the output

Your current directory should now contain the following:

```
.
├── redis ...................... input data directory
| ├── snapshot1.rdb............. redis snapshot part 1
| └── snapshot2.rdb............. redis snapshot part 2
|
├── extract .................... rdb->jsonl output directory
| ├── snapshot1.jsonl........... redis snapshot part 1, jsonl
| └── snapshot2.jsonl........... redis snapshot part 2, jsonl
|
├── aggregate .................. jsonl aggregate output directory
| └── merged.jsonl.............. redis snapshot part 1 + part 2, jsonl
|
├── validate-strict ............ validation output directory, strict mode
| ├── error..................... items that failed validation
| ├── log....................... validation report
| └── valid.jsonl............... items that passed validation
|
├── validate-lax ............... validation output directory, lax mode
| ├── error..................... items that failed validation
| ├── log....................... validation report
| └── valid.jsonl............... items that passed validation
```

The important new folders are `validate-strict` and `validate-lax`.

`validate-strict` flags data for any mismatch between Redis and Momento, if the data:

- exceeds the max item size, or
- has a TTL greater than the max TTL, or
- is missing a TTL (as this is required for Momento), or
- is a type unsupported by Momento

Eg you may wish to inspect data without TTL to understand what TTL to apply.

To contrast, `validate-lax` flags data for fewer of the criteria, ie if the data:

- exceeds the max item size, or
- is a type unsupported by Momento

This is helpful to catch only the data that is wholly unsupported. The other checks strict validation performs can be overcome, since we can apply a default TTL to items without one, clip the TTL of items that exceed the max TTL, and optionally reset the TTL of already expired items.

In the logs and in standard out you should see a report of the validation. The validation report will look something like this:

```
02:47:16 info: Momento.Etl.Cli.Validate.Command[0] ==== STATS ====
02:47:16 info: Momento.Etl.Cli.Validate.Command[0] Total: 214922
02:47:16 info: Momento.Etl.Cli.Validate.Command[0] OK: 214900
02:47:16 info: Momento.Etl.Cli.Validate.Command[0] Error: 22
02:47:16 info: Momento.Etl.Cli.Validate.Command[0] ----
02:47:16 info: Momento.Etl.Cli.Validate.Command[0] already_expired: 15
02:47:16 info: Momento.Etl.Cli.Validate.Command[0] no_ttl: 5
02:47:16 info: Momento.Etl.Cli.Validate.Command[0] data_too_large: 2
```

This means that 214900 passed the validation and there were 22 errors. Of the 22 errors, 15 items already expired, 5 items had no ttl, and 2 items were too large for the max size of 1MiB. Open the error file to see the particular items that failed validation and why.

## Load

Now we will load the data into Momento. We will use the `load` script. An example invocation is:

```
$ ./load.sh -a $AUTH_TOKEN -c $CACHE -t 1 -n 10 ./validate-lax/valid.jsonl
```

This will load the data in `./validate-lax/valid.jsonl` into the cache `$CACHE` with a default TTL of 1 day. The `-n` flag sets the number of concurrent requests to make to Momento.

Note:

- This will fail if the cache does not exist. We recommend creating the cache beforehand, since for a bulk load you will need to request a higher rate limit and throughput limit. See the [Momento docs](https://docs.momentohq.com) for more information.
- The service clips TTLs to be at most the cache-specific TTL.
- By default we discard items that have already expired. Run with `-r` to reset already expired items to the default TTL (Why would we want to override this? Suppose we are testing how long it takes to load a snapshot. As the snapshot ages, more and more items will expire. Eventually it becomes useless. Hence to test how long it takes to load a particular snapshot, we want to load expired items. This way we estimate the worst case.)
- Because the `load` script does upserts, you can run it multiple times on the same cache. That way if the operations is interrupted, or you cancel it, you can safely run it again from the start. To accomodate this, any time we load a list item, we delete that key first. This is because we do not want to append to the list, but rather replace it.

## (Optional) Verify the data in Momento matches what is on disk

We can also verify that the data we used the load step matches what is in Momento. The `verify` subcommand reads a json lines data file from disk, queries Momento for each of the items, and verifies the data matches. This is a sanity check that _should_ succeed, less items that already expired. To run, use this command:

`./bin/MomentoEtl verify -a $AUTH_TOKEN -c $CACHE -n 10 ./validate-lax/valid.jsonl`

Problematic lines are logged with error level logging.

# Run from an EC2 instance

We tested using an m6a.2xlarge with 64GB disk space, using `scripts/ec2-user-data.sh` to bootstrap the instance. We then used `make dist` to build the tool, copy to the instance, and run on the data. We recommend splitting the input file into a maximum of 10 chunks.
