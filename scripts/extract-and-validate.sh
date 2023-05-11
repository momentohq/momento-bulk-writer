#!/bin/bash
#set -x

function usage_exit() {
    echo "Usage: $0 [-s size] [-t ttl] [-b path] <data_path>";
    echo "  -s size     max item size, in MiB (default 1)";
    echo "  -t ttl      max ttl, in days (default 1)";
    echo "  -b path     path to momento etl binary (default linux-x64/MomentoEtl)";
    echo "  <data_path> path to data directory (where redis/ is expected and stage1/ will be created)";
    exit 1;
}

# Parse CLI args
max_item_size=1
max_ttl=1
momento_etl_path="linux-x64/MomentoEtl"

while getopts "hs:t:b:" o; do
    case "$o" in
        h)
            usage_exit
            ;;
        s)
            max_item_size=${OPTARG}
            ;;
        t)
            max_ttl=${OPTARG}
            ;;
        b)
            momento_etl_path=${OPTARG}
            ;;
        *)
            usage_exit
            ;;
    esac
done
shift $(($OPTIND-1))

# Assumes rdb files located at: $data_path/redis/*rdb
# This directory structure is necessary for the docker container
data_path=$1

if [ -z "$data_path" ]; then
  echo "Need to set data_path"
  usage_exit
fi

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

dir_exists_or_panic $data_path
file_exists_or_panic $momento_etl_path

echo "=== EXTRACT AND VALIDATE WITH THE FOLLOWING SETTINGS ==="
echo "max_item_size = $max_item_size"
echo "max_ttl = ${max_ttl}"
echo "momento_etl_path = ${momento_etl_path}"
echo "data_path = ${data_path}"

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
rm $joined_file 2> /dev/null

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
    $joined_file $stage3_strict_path/valid.jsonl $stage3_strict_path/error \
    2>&1 | tee $stage3_strict_path/log

if [ $? -ne 0 ]
then
    echo "Error running validate with strict settings: $?"
    exit 1
fi

# LAX
$momento_etl_path validate \
    --maxItemSize $max_item_size \
    $joined_file $stage3_lax_path/valid.jsonl $stage3_lax_path/error \
    2>&1 | tee $stage3_lax_path/log

if [ $? -ne 0 ]
then
    echo "Error running validate with lax settings: $?"
    exit 1
fi

echo Finished extract and validate. Inspect the validated data at $stage3_lax_path and $stage3_strict_path
