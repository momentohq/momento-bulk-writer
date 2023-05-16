#!/bin/bash
set -e

wget -O dist/redis-rdb-cli-release.tar.tgz https://github.com/leonchen83/redis-rdb-cli/releases/download/v0.9.3/redis-rdb-cli-release.tar.gz
tar xzvf dist/redis-rdb-cli-release.tar.tgz -C dist

for os_target in osx linux win; do
  runtime="${os_target}-x64"
  output_dir=momento-bulk-writer-$runtime
  mkdir -p dist/$output_dir/bin
  cp -r src/Momento.Etl/Cli/bin/Release/net6.0/$runtime/publish/* dist/$output_dir/bin
  mkdir -p dist/$output_dir/third-party
  cp -r dist/redis-rdb-cli dist/$output_dir/third-party
  cp scripts/{extract-rdb,extract-rdb-and-validate,validate,load}.sh dist/$output_dir

  if [ "$os_target" = "win" ]; then
    sed -i "" "s/bin\/MomentoEtl/bin\/MomentoEtl.exe/g" dist/$output_dir/*.sh
  fi

  cd dist && tar czvf $output_dir.tgz $output_dir && cd ..
done


