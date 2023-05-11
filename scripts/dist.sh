#!/bin/bash

for os_target in osx linux win; do
  runtime="${os_target}-x64"
  output_dir=momento-etl-$runtime
  mkdir -p dist/$output_dir/bin
  cp -r src/Momento.Etl/Cli/bin/Release/net6.0/$runtime/publish/* dist/$output_dir/bin
  cp scripts/{extract-and-validate,load}.sh dist/$output_dir

  if [ "$os_target" = "win" ]; then
    sed -i "" "s/bin\/MomentoEtl/bin\/MomentoEtl.exe/g" dist/$output_dir/*.sh
  fi

  cd dist && tar czvf $output_dir.tgz $output_dir && cd ..
done


