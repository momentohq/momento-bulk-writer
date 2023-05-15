#!/bin/bash
#set -x

function usage_exit() {
    echo "Usage: $0 [-s size] [-t ttl] <data_path> <output-path>";
    echo "  -s size       max item size, in MiB (default 1)";
    echo "  -t ttl        max ttl, in days (default 1)";
    echo "  <data_path>   path to directory with jsonl files to validate";
    echo "  <output-path> path to a directory where the output will be written";
    echo
    echo "Description: pipelines extract-rdb and validate.";
    echo "  See the individual script usage descriptions for more details.";
    exit 1;
}

# Parse CLI args
max_item_size=1
max_ttl=1

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

extract_path=$output_path/extract
./extract-rdb.sh $data_path $extract_path
./validate.sh -s $max_item_size -t $max_ttl $extract_path $output_path
