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
|       └── Cli ..................... cli to run validator
├── RdbToMomento.sln ................ repo solution file
├── LICENSE ......................... apache 2.0 license
└── README.md ....................... hey that's me

```
