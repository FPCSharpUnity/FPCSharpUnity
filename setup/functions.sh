# MyDir - directory where this script is.
md=`dirname $0`

# This should be filled in by setup.sh
libsrc=""

# Convert / to \.
wname() { echo $@ | sed -e "s|/|\\\\|g"; }

# Windows Directory Junction.
junction() {
  "$md/junction.exe" -accepteula "$@"
}

notif() {
  echo $@
  if [[ "$CI" != "" ]]; then
    echo "Running on CI, not asking for permission."
  else
    echo "Press ENTER to continue or CTRL+C to abort."
    read
  fi
}

realpath() {
  # osx does not have readlink -f, so we have this abomination
  echo "$(cd "$(dirname "$1")"; pwd)/$(basename "$1")"
}

# : separated list of files/dirs that were added using *link functions.
setup_files=""

add_setup_file() {
  if [ "$setup_files" != "" ]; then
    setup_files="$setup_files:"
  fi
  setup_files="$setup_files$1"
}

setup_gitignore() {
  local self="$0"

  echo "Setting up .gitignore"

  local gi=.gitignore
  local gi_gen=.gitignore.generated
  # https://stackoverflow.com/a/52748436/935259
  local setup_files_sorted="$(echo $setup_files | sed -e $'s/:/\\\n/g' | sort | paste -sd ':' -)"
  cat "$gi" | awk \
    -v "gen_start=# Generated by $self" \
    -v "gen_end=# End of Generated by $self" \
    -v "clean=$opt_clean" \
    -v "setup_files=$setup_files_sorted" \
'
BEGIN {
  existing_found=0
  autogen_section=0
  path_sep=":"
  gitignore_sep="\n"
  gsub(path_sep, gitignore_sep, setup_files)
}
{
  if (index($0, gen_start) != 0) {
    if (clean == 0) { print $0 }
    autogen_section=1
    existing_found=1
  }
  if (autogen_section == 0) print $0
  else if (index($0, gen_end) != 0) {
    autogen_section=0
    if (clean == 0) {
      print setup_files
      print $0
    }
  }
}
END {
  if (clean == 0 && existing_found == 0) {
    print "\n"
    print gen_start
    print setup_files
    print gen_end
  }
}
' > $gi_gen
  mv -f "$gi_gen" "$gi"
}

dirlink() {
  local name="$1"
  mkdir -p `dirname "$name"`

  if [[ "$OS" == *Windows* ]]; then
    junction -d "$name"
    test -e "$name" && {
      ls -la "$name"
      notif "Going to remove '$name'"
      rm -rfv "$name"
    }

    if [ "$opt_clean" != "1" ]; then
      while [ -n "$(junction "$name" "$libsrc/$name" | grep "Error opening")" ]; do
        echo "Directory $name is locked. Retrying in 1 second."
        sleep 1
        junction -d "$name"
      done
    fi
  else
    if [ -L "$name" ]; then
      rm "$name"
    elif [ -e "$name" ]; then
      ls -la "$name"
      notif "Going to remove '$name'"
      rm -rfv "$name"
    fi

    if [ "$opt_clean" != "1" ]; then
      ln -sv "$(realpath "$libsrc/$name")" "$name"
    fi
  fi

  add_setup_file "$name"
}

# Recursive dir link - find all dirs in given name and link them.
rdirlink() {
  local name="$1"
  for f in $(find "$libsrc/$name" -type d -mindepth 1 -maxdepth 1 | xargs); do
    local tname=`echo $f | sed -e "s|$libsrc/||"`
    dirlink "$tname"
  done
}

filelink() {
  local name="$1"
  mkdir -p `dirname $name`
  test -e "$name" && rm -rfv "$name"

  if [[ "$opt_clean" != "1" ]]; then
    if [[ "$OS" == *Windows* ]]; then
      fsutil hardlink create "$name" "$libsrc/$name"
    else
      local ctx=$(ctx $(dirname "$name"))
      ln -f "$libsrc/$name" "$name"
    fi
  fi

  add_setup_file "$name"
}

# Recursive file link - find all files in given name and link them.
rfilelink() {
  local name="$1"
  for f in $(find "$libsrc/$name" -type f | xargs); do
    local tname=`echo $f | sed -e "s|$libsrc/||"`
    filelink "$tname"
  done
}

opt_clean=0
for arg in "$@"; do
  if [ "$arg" == "clean" ]; then
    opt_clean=1
  fi
done
