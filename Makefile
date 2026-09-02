# ShareX-slim build entry points. Everything runs in a disposable container;
# nothing needs to be installed on the host except Docker.

export DOCKER_UID := $(shell id -u)
export DOCKER_GID := $(shell id -g)

COMPOSE  := docker compose
RUN      := $(COMPOSE) run --rm dotnet
SLN      ?= ShareX.Slim.sln
CONFIG   ?= Release
PLATFORM ?= x64

.PHONY: help image restore build rebuild clean shell destroy

help:
	@echo "make image    - build the .NET SDK container image"
	@echo "make restore  - restore NuGet packages (cached in a named volume)"
	@echo "make build    - build $(SLN) ($(CONFIG)|$(PLATFORM))"
	@echo "make rebuild  - clean + build"
	@echo "make clean    - remove build intermediates"
	@echo "make shell    - interactive shell in the build container"
	@echo "make destroy  - remove containers, image and cache volumes"

image:
	$(COMPOSE) build dotnet

restore: image
	$(RUN) restore $(SLN) -p:Platform=$(PLATFORM)

build: image
	$(RUN) build $(SLN) -c $(CONFIG) -p:Platform=$(PLATFORM)

rebuild: clean build

clean: image
	$(RUN) clean $(SLN) -c $(CONFIG) -p:Platform=$(PLATFORM)

shell:
	$(COMPOSE) run --rm shell

destroy:
	$(COMPOSE) down --volumes --remove-orphans
	-docker image rm sharex-slim-build:local
