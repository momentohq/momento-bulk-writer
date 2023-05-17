# Project Structure

```
.
├── data ............................ sample data
│ └── redis ....................... path to write redis snapshots
├── scripts.......................... scripts run all or part of the pipeline
├── src .............................
│ └── Momento.Etl .................
│ ├── RedisLoadGenerator ...... redis load gen to create rdb files
│ ├── RedisReload ............. redis reload data with default ttl
| ├── Model ................... redis data model basd on redis-rdb-cli jsonl
| ├── Validation .............. momento-redis data validation
| └── Cli ..................... cli to run validator, loader, and verifier
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
