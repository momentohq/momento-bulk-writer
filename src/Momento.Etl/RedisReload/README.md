# Redis Reloader

Redis backups lose testing value as time goes on because items expire. This project loads a Redis dump (as jsonl) into Redis. To account for expired items, this replaces the TTL of all items with a large default TTL.
