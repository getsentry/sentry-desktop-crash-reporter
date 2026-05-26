.DEFAULT_GOAL := help

DOTNET ?= dotnet

PROJECT := Sentry.CrashReporter/Sentry.CrashReporter.csproj
SOLUTION := Sentry.CrashReporter.sln
UNIT_TEST_PROJECT := tests/Sentry.CrashReporter.Tests/Sentry.CrashReporter.Tests.csproj
UI_TEST_PROJECT := tests/Sentry.CrashReporter.UITests/Sentry.CrashReporter.UITests.csproj
RUNTIME_TEST_HOST_PROJECT := tests/Sentry.CrashReporter.RuntimeTests.Host/Sentry.CrashReporter.RuntimeTests.Host.csproj

CONFIG ?=
FRAMEWORK ?= net10.0-desktop
RID ?= $(shell $(DOTNET) --info | awk '$$1 == "RID:" { print $$2; exit }')
OUTPUT ?=

RUNTIME_TEST_OUTPUT := tests/Sentry.CrashReporter.RuntimeTests/TestResults
RUNTIME_TEST_HOST_DLL = tests/Sentry.CrashReporter.RuntimeTests.Host/bin/$(or $(CONFIG),Debug)/net10.0-desktop/Sentry.CrashReporter.RuntimeTests.Host.dll

ifneq ($(filter run test,$(firstword $(MAKECMDGOALS))),)
ARGS := $(wordlist 2,$(words $(MAKECMDGOALS)),$(MAKECMDGOALS))
.PHONY: $(ARGS)
$(ARGS):
	@:
endif

.PHONY: help restore build run publish test test-runtime test-ui

help:
	@printf "Targets:\n"
	@printf "  make build          Build the desktop app ($(or $(CONFIG),Debug), $(FRAMEWORK))\n"
	@printf "  make run            Run the desktop app ($(or $(CONFIG),Debug), $(FRAMEWORK))\n"
	@printf "  make publish        Publish the desktop app ($(or $(CONFIG),Release), $(FRAMEWORK), $(RID))\n"
	@printf "  make test           Run unit tests ($(or $(CONFIG),Debug))\n"
	@printf "  make test-runtime   Build and run Uno runtime tests under xvfb ($(or $(CONFIG),Debug))\n"
	@printf "  make test-ui        Build and run WebAssembly UI tests ($(or $(CONFIG),Debug))\n"
	@printf "\n"
	@printf "Variables:\n"
	@printf "  CONFIG              Debug/Release\n"
	@printf "  RID                 win-x64/osx-arm64/linux-x64/...\n"
	@printf "  OUTPUT              publish output path\n"
	@printf "\n"
	@printf "Common overrides:\n"
	@printf "  make run -- path/to/file.envelope\n"
	@printf "  make test -- --filter FullyQualifiedName~Submit\n"
	@printf "  make publish RID=osx-arm64\n"
	@printf "  make publish RID=win-x64 OUTPUT=build\n"
	@printf "  make build CONFIG=Release\n"

restore:
	$(DOTNET) restore $(SOLUTION)

build:
	$(DOTNET) build -c $(or $(CONFIG),Debug) -f $(FRAMEWORK) $(PROJECT)

run:
	$(DOTNET) run --project $(PROJECT) -c $(or $(CONFIG),Debug) -f $(FRAMEWORK) -- $(or $(ARGS),tests/data/inproc.envelope)

publish:
	$(DOTNET) publish -c $(or $(CONFIG),Release) -f $(FRAMEWORK) -r $(RID) $(PROJECT) $(if $(OUTPUT),-o $(OUTPUT))


test:
	$(DOTNET) build $(UNIT_TEST_PROJECT) /p:Configuration=$(or $(CONFIG),Debug) /p:OverrideTargetFramework=net10.0
	$(DOTNET) test $(UNIT_TEST_PROJECT) --no-build -c $(or $(CONFIG),Debug) --blame-crash $(ARGS)

test-runtime:
	$(DOTNET) build -c $(or $(CONFIG),Debug) -f net10.0-desktop $(RUNTIME_TEST_HOST_PROJECT)
	UNO_RUNTIME_TESTS_RUN_TESTS='{}' UNO_RUNTIME_TESTS_OUTPUT_PATH=$(RUNTIME_TEST_OUTPUT)/RuntimeTests.xml xvfb-run -a $(DOTNET) $(RUNTIME_TEST_HOST_DLL)

test-ui:
	$(DOTNET) build -c $(or $(CONFIG),Debug) -f net10.0-browserwasm -p:IsUiAutomationMappingEnabled=True $(PROJECT)
	$(DOTNET) build -c $(or $(CONFIG),Debug) -f net10.0 $(UI_TEST_PROJECT)
	pwsh .github/scripts/run-ui-tests.ps1 -Configuration $(or $(CONFIG),Debug)
