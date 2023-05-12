#!/bin/bash

# Load a single file into Momento

#set -x

function usage_exit() {
    echo "Usage: $0 -a token -c cache [-t ttl] [-n N] [-r] [-l path] data_path"
    echo "  -a token    Momento auth token"
    echo "  -c cache    Momento cache name"
    echo "  -t ttl      default ttl for Momento cache, in days (defaults 1)"
    echo "  -n N        number of concurrent requests to make to Momento (defaults to 4)"
    echo "  -r          reset expired item ttl to default (defaults to false)"
    echo "  -l path     path to write the log to (defaults to ./load.log)"
    echo "  <data_path> path to data file to load"
    exit 1
}

default_ttl=1
num_concurrent_requests=4
momento_etl_path="bin/MomentoEtl"
log_path="load.log"
reset_expired_items=0

while getopts "ha:c:t:n:l:r" o; do
    case "$o" in
        h)
            usage_exit
            ;;
        a)
            auth_token=${OPTARG}
            ;;
        c)
            cache_name=${OPTARG}
            ;;
        t)
            default_ttl=${OPTARG}
            ;;
        n)
            num_concurrent_requests=${OPTARG}
            ;;
        r)
            reset_expired_items=1
            ;;
        l)
            log_path=${OPTARG}
            ;;
        *)
            usage_exit
            ;;
    esac
done
shift $(($OPTIND-1))

data_path=$1

function verify_set()
{
    if [ "$1" = "" ]
    then
        echo "Need to set cl arg: $2 not set"
        usage_exit
    fi
}

function file_exists_or_panic() {
    if [ ! -f "$1" ]; then
        echo "file $1 does not exist but should; bailing."
        exit 1
    fi
}

verify_set "$auth_token" "auth_token"
verify_set "$cache_name" "cache_name"
verify_set "$data_path" "data_path"
file_exists_or_panic $data_path
file_exists_or_panic $momento_etl_path

echo "=== LOADING WITH THE FOLLOWING SETTINGS ==="
echo "auth_token = **** [censored]"
echo "cache_name = $cache_name"
echo "default_ttl = $default_ttl"
echo "num_concurrent_requests = $num_concurrent_requests"
echo "momento_etl_path = $momento_etl_path"
echo "log_path = $log_path"
echo "reset_expired_items = $reset_expired_items"
echo "data_path = $data_path"

if [ $reset_expired_items -eq 1 ]
then
    reset_expired_items="-r"
else
    reset_expired_items=""
fi

echo

$momento_etl_path load \
  -a $auth_token \
	-c $cache_name \
	--defaultTtl $default_ttl \
  -n $num_concurrent_requests \
  $reset_expired_items \
	$data_path 2>&1 | tee $log_path

if [ $? -ne 0 ]
then
    echo "Error loading: $?"
fi

echo "Done loading"
