#!/bin/bash
#set -x

function usage_exit() {
    echo "Usage: $0 [-s size] [-t ttl] <data_path> <output-path>";
    echo "  -s size       max item size, in MiB (default 1)";
    echo "  -t ttl        max ttl, in days (default 1)";
    echo "  <data_path>   path to directory with jsonl files to validate";
    echo "  <output-path> path to a directory where the output will be written";
    echo
    echo "Description: Validates jsonl files using the momento-etl tool.";
    echo "  jsonl files are assumed to be located at: <data_path>/*jsonl";
    echo "  The tool first aggregates the data to <output_path>/aggregate,";
    echo "  then validates the data with strict and lax settings to";
    echo "  <output_path>/validate-strict and <output_path>/validate-lax respectively.";
    echo
    echo "  Strict validation tells you if any data meets the following:";
    echo "    - exceeds the max item size";
    echo "    - has a ttl greater than the max ttl";
    echo "    - is missing a ttl (as this is required for Momento)";
    echo "    - is a type unsupported by Momento";
    echo "  This is helpful to cast a wide net and find any data that is not";
    echo "  supported by Momento or potentially problematic (eg why does that";
    echo "  item have an expiry of 2 years?)";
    echo
    echo "  Lax (relaxed) validation tells you if any data meets the following:";
    echo "    - exceeds the max item size";
    echo "    - is a type unsupported by Momento";
    echo "  This is helpful to see the data that is truly unsupported, as we can";
    echo "  apply a default TTL to items without one, clip the TTL of items that";
    echo "  exceed the max TTL, and optionally reset the TTL of already expired items.";
    exit 1;
}

# Parse CLI args
max_item_size=1
max_ttl=1
momento_etl_path="bin/MomentoEtl"

while getopts "hs:t:" o; do
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
        *)
            usage_exit
            ;;
    esac
done
shift $(($OPTIND-1))

# Assumes rdb files located at: $data_path/redis/*rdb
# This directory structure is necessary for the docker container
data_path=$1
output_path=$2

if [ -z "$data_path" ]; then
  echo "Need to set data_path"
  usage_exit
fi

if [ -z "$output_path" ]; then
  echo "Need to set output_path"
  usage_exit
fi

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
create_path_or_panic $output_path
file_exists_or_panic $momento_etl_path

echo "=== VALIDATE WITH THE FOLLOWING SETTINGS ==="
echo "max_item_size = $max_item_size"
echo "max_ttl = ${max_ttl}"
echo "momento_etl_path = ${momento_etl_path}"
echo "data_path = ${data_path}"
echo "output_path = ${output_path}"

aggregate_path=$output_path/aggregate
strict_path=$output_path/validate-strict
lax_path=$output_path/validate-lax

create_path_or_panic $aggregate_path
create_path_or_panic $strict_path
create_path_or_panic $lax_path

# Flush any data from previous runs
rm -f $aggregate_path/* $strict_path/* $lax_path/* 2> /dev/null

###############
# AGGREGATE JSONL
###############

# Having the data in one file makes looking at the validation results easier
joined_file=$aggregate_path/merged.jsonl
rm $joined_file 2> /dev/null

echo
echo ==== AGGREGATE JSONL
echo "Aggregating jsonl files from $data_path into $joined_file"

for file in `ls $data_path/*jsonl`
do
    cat $file >> $joined_file
    # Ensure newline between files
    echo >> $joined_file
done

###############
#  VALIDATE
###############
echo
echo ==== VALIDATE

# STRICT
$momento_etl_path validate \
    --maxItemSize $max_item_size \
    --maxTtl $max_ttl --filterLongTtl \
    --filterAlreadyExpired \
    --filterMissingTtl \
    $joined_file $strict_path/valid.jsonl $strict_path/error \
    2>&1 | tee $strict_path/log

if [ $? -ne 0 ]
then
    echo "Error running validate with strict settings: $?"
    exit 1
fi

# LAX
$momento_etl_path validate \
    --maxItemSize $max_item_size \
    $joined_file $lax_path/valid.jsonl $lax_path/error \
    2>&1 | tee $lax_path/log

if [ $? -ne 0 ]
then
    echo "Error running validate with lax settings: $?"
    exit 1
fi

echo Finished transform and validate. Inspect the validated data at $lax_path and $strict_path
