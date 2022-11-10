#!/bin/bash

# Load a single file into Momento by:
# - splitting into multiple chunks
# - loading the chunks in parallel

set -x

# Path to validated file
data_path=$1

# Path to MomentoEtl binary
momento_etl_path=$2

# Momento auth token
auth_token=$3

# Cache name to load data to
cache_name=$4

# Default TTL in days
default_ttl=$5

# Max TTL in days
max_ttl=$6
log_dir=${7:-logs}
temp_dir=${8:-temp}

function file_exists_or_panic() {
    if [ ! -f "$1" ]; then
        echo "file $1 does not exist but should; bailing."
        exit 1
    fi
}

function dir_not_exists_or_panic() {
    if [ -d "$1" ]; then
        echo "directory $1 should not exist; remove before starting; bailing."
        exit 1
    fi
}

file_exists_or_panic $data_path
dir_not_exists_or_panic $temp_dir
mkdir -p $temp_dir



filename=$(basename $data_path)
split -l 10 $data_path $temp_dir/$filename

for file in `ls $temp_dir/${filename}*`
do
    ./load_one.sh $file $momento_etl_path $auth_token $cache_name $default_ttl $max_ttl $log_dir &
done
