#!/bin/bash
set -x

mountpoint=${1:-../data}
rdb_relative_to_mountpoint=${2:-redis/dump.rdb}
jsonl_relative_to_mountpoint=${3:-dump.jsonl}

# Expand to absolute path
mountpoint=$(readlink -f $mountpoint)

echo Reading from $(readlink -f "$mountpoint/$rdb_relative_to_mountpoint") and saving to $(readlink -f "$mountpoint/$jsonl_relative_to_mountpoint")

docker run \
    -w /app/redis-cli/bin \
    -v "$mountpoint:/data" \
    redisrdbcli/redis-rdb-cli:latest \
    rct -f jsonl -s /data/$rdb_relative_to_mountpoint -o /data/$jsonl_relative_to_mountpoint
