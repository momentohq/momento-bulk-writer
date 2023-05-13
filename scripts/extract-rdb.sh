#!/bin/bash
#set -x

function usage_exit() {
    echo "Usage: $0 <data_path> <output-path>";
    echo "  <data_path>   path to a directory with rdb files";
    echo "  <output-path> path to a directory where the output will be written";
    echo
    echo "Description: Extracts rdb files to jsonl files using the rct tool.";
    echo "  rdb files are assumed to be located at: <data_path>/*rdb";
    echo "  jsonl files will be written to: <output_path>/*jsonl";
    exit 1;
}

# Parse CLI args
rdb_cli_path="third-party/redis-rdb-cli/bin/rct"

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

data_path=$(readlink -f $data_path)


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
output_path=$(readlink -f $output_path)
file_exists_or_panic $rdb_cli_path

echo "=== EXTRACT RDB WITH THE FOLLOWING SETTINGS ==="
echo "data_path = ${data_path}"
echo "output_path = ${output_path}"


# Flush any data from previous runs
rm -f $output_path/*jsonl

###############
# RDB -> JSONL
###############

echo
echo ==== EXTRACT RDB TO JSONL

# Because the rct tool assumes it is run from the bin directory
# we need to change to that directory before running it
pushd "$(dirname $rdb_cli_path)" > /dev/null
rdb_cli_filename="$(basename $rdb_cli_path)"

for file in `ls $data_path/*rdb`
do
    rdb_filename="$(basename $file)"
    jsonl_filename="${rdb_filename%.*}.jsonl"

    # Get the directory that the rdb file is in
    # This is necessary because rdb-cli will create a file in the current directory
    # and we want to put it in the stage1 directory
    ./${rdb_cli_filename} -f jsonl -s $file -o $output_path/$jsonl_filename

    if [ $? -ne 0 ]; then
        echo "Error converting RDB to JSONL, bailing: $?"
        exit 1
    fi
done
popd > /dev/null

echo Finished extract extract-rdb. Inspect the data at $output_path
