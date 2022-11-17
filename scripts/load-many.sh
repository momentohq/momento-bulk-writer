#!/bin/bash

# Load a single file into Momento by:
# - splitting into multiple chunks
# - loading the chunks in parallel

set -x

# Path to validated file
data_path=$1

# Momento auth token
auth_token=$2

# Cache name to load data to
cache_name=$3

# Default TTL in days
default_ttl=$4

num_chunks=${5:-10}

# Path to MomentoEtl binary
momento_etl_path=${6:-linux-x64/MomentoEtl}

temp_root=$(mktemp -u -d -t load-many-$(date +%Y-%m-%d-%H-%M-%S)-XXXXXXXXXX)
log_dir=${7:-$temp_root/logs}
temp_dir=${8:-$temp_root/data}


function file_exists_or_panic() {
    if [ ! -f "$1" ]; then
        echo "file $1 does not exist but should; bailing."
        exit 1
    fi
}

function dir_exists_or_panic() {
    if [! -d "$1" ]; then
        echo "directory $1 should not exist; remove before starting; bailing."
        exit 1
    fi
}

file_exists_or_panic $data_path
mkdir -p $temp_dir $log_dir
dir_exists_or_panic $temp_dir
dir_exists_or_panic $log_dir


filename=$(basename $data_path)
$momento_etl_path split -n $num_chunks $data_path $temp_dir/$filename

for file in `ls $temp_dir/${filename}*`
do
    ./load-one.sh $file $auth_token $cache_name $default_ttl $momento_etl_path $log_dir &
done
