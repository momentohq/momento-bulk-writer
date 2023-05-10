#!/bin/bash

# Load a single file into Momento

set -x

# Path to validated file
data_path=$1

# Momento auth token
auth_token=$2

# Cache name to load data to
cache_name=$3

# Default TTL in days
default_ttl=$4

# Number of concurrent requests to make
num_concurrent_requests=${5:-10}

# Path to MomentoEtl binary
momento_etl_path=${6:-linux-x64/MomentoEtl}

log_dir=${7:-logs}


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

file_exists_or_panic $data_path
file_exists_or_panic $momento_etl_path
is_set_or_panic $auth_token "auth_token"
is_set_or_panic $cache_name "cache_name"
is_set_or_panic $default_ttl "default_ttl"
if [ "$log_dir" = "logs" ]
then
    mkdir -p $log_dir
fi
dir_exists_or_panic $log_dir

log_path="$log_dir/$(basename $data_path).log"

$momento_etl_path load \
  -a $auth_token \
	-c $cache_name \
	--defaultTtl $default_ttl \
  -n $num_concurrent_requests \
	$data_path 2>&1 > $log_path

if [ $? -ne 0 ]
then
    echo "Error loading: $?"
fi
