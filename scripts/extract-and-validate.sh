#!/bin/bash
set -x

# Assumes rdb files located at: $data_path/redis/*rdb
# This directory structure is necessary for the docker container
data_path=$1

# Max item size in MiB
max_item_size=$2

# Max TTL in days
max_ttl=$3

# Path to MomentoEtl binary
momento_etl_path=${4:-linux-x64/MomentoEtl}


# Assumes rdb files located at: $data_path/redis/*rdb
# Writes rdb -> jsonl at $data_path/stage1
# - Joins to single jsonl file in $data_path/stage2
# - Validates with strict and lax settings to
#   - $data_path/stage3_strict and $data_path/stage3_lax respectively

function create_path_or_panic() {
    mkdir -p $1
    if [ $? -ne 0 ]
    then
        echo Could not create $1
        exit 1
    fi

}

function dir_exists_or_panic() {
    if [ ! -d "$1" ]; then
        echo "directory $1 does not exist but should; bailing."
        exit 1
    fi
}

function file_exists_or_panic() {
    if [ ! -f "$1" ]; then
        echo "file $1 does not exist but should; bailing."
        exit 1
    fi
}

function is_set_or_panic() {
    if [ "$1" = "" ]
    then
        echo "Need to set config in file: $2 not set"
        exit 1
    fi
}

dir_exists_or_panic $data_path
file_exists_or_panic $momento_etl_path
is_set_or_panic "$max_item_size" "max_item_size"
is_set_or_panic "$max_ttl" "max_ttl"

stage1_path=$data_path/stage1
stage2_path=$data_path/stage2
stage3_strict_path=$data_path/stage3-strict
stage3_lax_path=$data_path/stage3-lax

create_path_or_panic $stage1_path
create_path_or_panic $stage2_path
create_path_or_panic $stage3_strict_path
create_path_or_panic $stage3_lax_path

# Flush any data from previous runs
rm -f $stage1_path/* $stage2_path/* $stage3_strict_path/* $stage3_lax_path/*

###############
# STAGE 1: RDB -> JSONL
###############

redis_path=$data_path/redis
dir_exists_or_panic $redis_path

# Expand to absolute path
mountpoint=$(readlink -f $data_path)

echo ==== STAGE 1: RDB TO JSONL

for file in `ls $redis_path/*rdb`
do
    rdb_filename="$(basename $file)"
    jsonl_filename="${rdb_filename%.*}.jsonl"

    docker run \
        -w /app/redis-cli/bin \
        -v "$mountpoint:/data" \
        redisrdbcli/redis-rdb-cli:latest \
        rct -f jsonl -s /data/redis/$rdb_filename -o /data/stage1/$jsonl_filename
    
    if [ $? -ne 0 ]; then
        echo "Error converting RDB to JSONL, bailing: $?"
        exit 1
    fi
done

###############
# STAGE 2: JOIN JSONL
###############

# Having the data in one file makes looking at the validation results easier
joined_file=$stage2_path/merged.jsonl
rm $joined_file

echo ==== STAGE 2: JOIN JSONL

for file in `ls $stage1_path/*jsonl`
do
    cat $file >> $joined_file
    # Ensure newline between files
    echo >> $joined_file
done

###############
# STAGE 3: VALIDATE
###############

# STRICT
$momento_etl_path validate \
    --maxItemSize $max_item_size \
    --maxTtl $max_ttl --filterLongTtl \
    --filterAlreadyExpired \
    --filterMissingTtl \
    $joined_file $stage3_strict_path/valid $stage3_strict_path/error \
    2>&1 | tee $stage3_strict_path/log

if [ $? -ne 0 ]
then
    echo "Error running validate with strict settings: $?"
    exit 1
fi

# LAX
$momento_etl_path validate \
    --maxItemSize $max_item_size \
    $joined_file $stage3_lax_path/valid $stage3_lax_path/error \
    2>&1 | tee $stage3_lax_path/log

if [ $? -ne 0 ]
then
    echo "Error running validate with lax settings: $?"
    exit 1
fi

echo Finished extract and validate. Inspect the validated data at $stage3_lax_path and $stage3_strict_path
