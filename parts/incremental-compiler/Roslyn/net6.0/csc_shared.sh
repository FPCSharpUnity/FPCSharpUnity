#!/usr/bin/env bash
# Used by .csproj files to make compilation faster

SCRIPT_DIR="$( cd -- "$( dirname -- "${BASH_SOURCE[0]:-$0}"; )" &> /dev/null && pwd 2> /dev/null; )";

dotnet "$SCRIPT_DIR/csc.dll" -shared "$@"