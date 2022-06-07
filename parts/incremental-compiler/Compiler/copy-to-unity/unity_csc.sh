#!/bin/zsh

# ###############################################
# ### This script is only intended for macos. ###
# ###############################################

# Source the profile configuration file to make sure our environment is as close to
# users shell as possible (for example the $PATH).
if [[ -f "$HOME/.zprofile" ]]; then
  source "$HOME/.zprofile"
fi
if [[ -f "$HOME/.zshrc" ]]; then
  source "$HOME/.zshrc"
fi

# Add common paths for `dotnet` and `mono` to the $PATH
export PATH="$PATH:/opt/homebrew/bin:/usr/local/bin"

APPLICATION_CONTENTS=$(dirname "$0")/../..

if [ -f "$APPLICATION_CONTENTS/Tools/Roslyn/csc" ];
then
    CSC_NET_CORE=$APPLICATION_CONTENTS/Tools/Roslyn/csc
else
    TOPLEVEL=$(dirname "$0")/../../../..
    CSC_NET_CORE=$TOPLEVEL/artifacts/buildprogram/Stevedore/roslyn-csc-mac/csc
fi

CURRENT_DIR=$(dirname "$0")

eval "/Library/Frameworks/Mono.framework/Commands/mono \"$CURRENT_DIR/csc.wrapper.exe\" "$@""

EXITCODE=$?

if [ $EXITCODE -eq 228 ]
then
    eval "\"$CSC_NET_CORE\" /shared "$@""
    exit $?
fi

exit $EXITCODE
