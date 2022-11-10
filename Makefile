.PHONY: clean
## Clean project
clean:
	@dotnet clean
	@rm -rf dist/*

.PHONY: test
## Run unit tests
test:
	@dotnet test

.PHONY: publish
## Run dotnet publish to build standalone executables
publish:
	@dotnet publish src/Momento.Etl/Cli -c Release -r linux-x64 -p:PublishSingleFile=true --self-contained false
	@dotnet publish src/Momento.Etl/Cli -c Release -r osx-x64 -p:PublishSingleFile=true --self-contained false

.PHONY: dist
## Package up executables and scripts for deployment
dist: clean publish
	@mkdir -p dist/momento_etl/linux-x64 dist/momento_etl/osx-x64
	@cp -r src/Momento.Etl/Cli/bin/Release/net6.0/linux-x64/publish/* dist/momento_etl/linux-x64
	@cp -r src/Momento.Etl/Cli/bin/Release/net6.0/osx-x64/publish/* dist/momento_etl/osx-x64
	@cp scripts/extract_and_validate.sh dist/momento_etl
	@cp scripts/load_one.sh dist/momento_etl
	@cp scripts/load_many.sh dist/momento_etl
	@cd dist && tar czvf momento_etl.tgz momento_etl/*

# See <https://gist.github.com/klmr/575726c7e05d8780505a> for explanation.
.PHONY: help
help:
	@echo "$$(tput bold)Available rules:$$(tput sgr0)";echo;sed -ne"/^## /{h;s/.*//;:d" -e"H;n;s/^## //;td" -e"s/:.*//;G;s/\\n## /---/;s/\\n/ /g;p;}" ${MAKEFILE_LIST}|LC_ALL='C' sort -f|awk -F --- -v n=$$(tput cols) -v i=19 -v a="$$(tput setaf 6)" -v z="$$(tput sgr0)" '{printf"%s%*s%s ",a,-i,$$1,z;m=split($$2,w," ");l=n-i;for(j=1;j<=m;j++){l-=length(w[j])+1;if(l<= 0){l=n-i-length(w[j])-1;printf"\n%*s ",-i," ";}printf"%s ",w[j];}printf"\n";}'|more $(shell test $(shell uname) == Darwin && echo '-Xr')
