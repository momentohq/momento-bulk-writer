# Overview

Load generator for Redis. In order to generate snapshots with a variety of items, data types, sizes, and expiries, we provide a configurable console app to generate data.

# How to Run

To see the various configuration options, run:

```bash
dotnet run -- --help
```

To store the data in an isolated Redis, run:

```bash
docker compose up
```

Once the load generator finishes, telnet to Redis and issue a save:

```bash
> telnet 127.0.0.1 6379
Trying 127.0.0.1...
Connected to 127.0.0.1.
Escape character is '^]'.
SAVE
+OK
```

The file `dump.rdb` will be in the repo root folder `/data/redis/`.

# Expected RDB file sizes

Using the default configuration, to produce a ~1GB rdb, use 34_500 items.

Using the same default configuration, to produce ~15GB rdb, use 500_000 items.
