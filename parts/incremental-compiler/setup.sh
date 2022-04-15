#!/bin/sh

# Copy compiler directories instead of linking them, 
# because sometimes it gets messed up and non-developers 
# have a hard time resetting the submodule
dircopy Compiler
dircopy Roslyn

dirlink "Assets/CSharp vNext Support"
dirlink "Assets/Plugins/Incremental Compiler"
