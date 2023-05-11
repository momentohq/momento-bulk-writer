.PHONY: clean
## Clean project
clean:
	@dotnet clean
	@rm -rf dist/*

.PHONY: build
## Build project
build:
	@dotnet build

.PHONY: format
## Format code
format:
	@dotnet format

.PHONY: test
## Run unit tests
test:
	@dotnet test

.PHONY: precommit
## Run clean-build, format, and test as a step before committing.
precommit: build format test

.PHONY: publish
##  Build standalone executables
publish:
	@dotnet publish src/Momento.Etl/Cli -c Release -r linux-x64 -p:PublishSingleFile=true --self-contained
	@dotnet publish src/Momento.Etl/Cli -c Release -r osx-x64 -p:PublishSingleFile=true --self-contained
	@dotnet publish src/Momento.Etl/Cli -c Release -r win-x64 -p:PublishSingleFile=true --self-contained

.PHONY: dist
## Package up executables and scripts for deployment
dist: clean publish
	@./scripts/dist.sh

# See <https://gist.github.com/klmr/575726c7e05d8780505a> for explanation.
.PHONY: help
help:
	@echo "$$(tput bold)Available rules:$$(tput sgr0)";echo;sed -ne"/^## /{h;s/.*//;:d" -e"H;n;s/^## //;td" -e"s/:.*//;G;s/\\n## /---/;s/\\n/ /g;p;}" ${MAKEFILE_LIST}|LC_ALL='C' sort -f|awk -F --- -v n=$$(tput cols) -v i=19 -v a="$$(tput setaf 6)" -v z="$$(tput sgr0)" '{printf"%s%*s%s ",a,-i,$$1,z;m=split($$2,w," ");l=n-i;for(j=1;j<=m;j++){l-=length(w[j])+1;if(l<= 0){l=n-i-length(w[j])-1;printf"\n%*s ",-i," ";}printf"%s ",w[j];}printf"\n";}'|more $(shell test $(shell uname) == Darwin && echo '-Xr')
