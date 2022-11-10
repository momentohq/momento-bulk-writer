#!/bin/bash

# Load a single file into Momento

set -x

data_path=$1
momento_etl_path=$2
auth_token=$3
cache_name=$4
default_ttl=$5
max_ttl=$6
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
is_set_or_panic $max_ttl "max_ttl"
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
	--maxTtl $max_ttl \
	$data_path 2>&1 > $log_path

if [ $? -ne 0 ]
then
    echo "Error loading: $?"
fi
