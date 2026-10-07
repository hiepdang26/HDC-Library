#!/bin/bash
# Compiles HDCLib's assemblies the way Unity does for each setup a game can have, with the Roslyn compiler of
# the project's Unity version, in seconds and without opening Unity. One line per assembly and setup:
#   common         HDC.Ads.Settings, built in every setup
#   editor         HDC ads on, in the Editor with iOS as the build target, with the Edit Mode tests
#   ios, android   HDC ads on with Firebase and Adjust, in a player build
#   nofirebase     HDC ads on without Firebase or Adjust, with the Edit Mode tests
#   inputsystem    the debug panel with the Input System package as the only input
#   off            HDC ads off: the assemblies that build anyway, HDCAdjust with and without Adjust
#   editorios, editorandroid   the Editor assembly with each build target
#   editor, android (HDC.Ads.TestScenes)   the test scenes in the Editor and in a player build
#
# Usage: Tools~/compile-matrix.sh [output folder]   (default: $TMPDIR/hdc-compile-matrix)
# Environment:
#   HDC_PROJECT       the Unity project to take references from (default: the one holding HDCLib)
#   UNITY_EDITOR_DIR  the Unity install, such as /Applications/Unity/Hub/Editor/2022.3.62f3
#                     (default: the project's version under Unity Hub's macOS folder)
# The project must have been opened once: UnityEngine.UI, the Input System, Adjust and the test runner come from
# its Library/ScriptAssemblies. A setup whose references the project lacks is skipped, not failed.

SRC=$(cd "$(dirname "$0")/.." && pwd)
PROJ=${HDC_PROJECT:-$SRC}
while [ "$PROJ" != / ] && [ ! -f "$PROJ/ProjectSettings/ProjectVersion.txt" ]; do PROJ=$(dirname "$PROJ"); done
[ -f "$PROJ/ProjectSettings/ProjectVersion.txt" ] || { echo "No Unity project holds $SRC; set HDC_PROJECT." >&2; exit 2; }
VERSION=$(sed -n 's/^m_EditorVersion: //p' "$PROJ/ProjectSettings/ProjectVersion.txt")
UNITY=${UNITY_EDITOR_DIR:-/Applications/Unity/Hub/Editor/$VERSION}
if [ -d "$UNITY/Unity.app/Contents" ]; then C=$UNITY/Unity.app/Contents; ENGINES=$UNITY/PlaybackEngines
else C=$UNITY/Editor/Data; ENGINES=$C/PlaybackEngines; fi
DOTNET=$C/NetCoreRuntime/dotnet; [ -x "$DOTNET" ] || DOTNET=$C/NetCoreRuntime/dotnet.exe
CSC=$C/DotNetSdkRoslyn/csc.dll
[ -f "$CSC" ] || { echo "No Roslyn compiler in $UNITY; set UNITY_EDITOR_DIR." >&2; exit 2; }
OUT=${1:-${TMPDIR:-/tmp}/hdc-compile-matrix}
G=$OUT/.refs
rm -rf "$OUT"; mkdir -p "$G"
echo "HDCLib $SRC"
echo "Unity  $VERSION ($UNITY)"

# Unity's UNITY_<version>_OR_NEWER defines for the project's version.
MAJOR=${VERSION%%.*}; MINOR=${VERSION#*.}; MINOR=${MINOR%%.*}
DEFINES="UNITY_$MAJOR;UNITY_${MAJOR}_$MINOR;UNITY_5_3_OR_NEWER;UNITY_5_4_OR_NEWER;UNITY_5_5_OR_NEWER;UNITY_5_6_OR_NEWER"
for year in 2017 2018 2019 2020 2021 2022 2023; do
  for minor in 1 2 3 4; do
    if [ "$MAJOR" -gt "$year" ] || { [ "$MAJOR" -eq "$year" ] && [ "$minor" -le "$MINOR" ]; }; then
      DEFINES="$DEFINES;UNITY_${year}_${minor}_OR_NEWER"
    fi
  done
done
if [ "$MAJOR" -ge 6000 ]; then
  for minor in $(seq 0 "$MINOR"); do DEFINES="$DEFINES;UNITY_6000_${minor}_OR_NEWER"; done
fi

# Reference groups: one file of -r: lines each, empty when the project lacks them.
group() { # name, then the DLLs on stdin
  grep -v '^$' | sed 's/.*/-r:"&"/' > "$G/$1"
}
printf '%s\n' "$C"/UnityReferenceAssemblies/unity-4.8-api/*.dll "$C"/UnityReferenceAssemblies/unity-4.8-api/Facades/*.dll | group bcl
printf '%s\n' "$C"/Managed/UnityEngine/UnityEngine*.dll | group engine
printf '%s\n' "$C"/Managed/UnityEngine/UnityEditor*.dll | group editor
find "$ENGINES/iOSSupport" -maxdepth 1 -name 'UnityEditor.iOS.Extensions.*.dll' 2>/dev/null | sort | group xcode
find "$PROJ/Assets/GoogleMobileAds" "$PROJ"/Library/PackageCache/com.google.ads.mobile* -maxdepth 2 -name 'GoogleMobileAds*.dll' -not -path '*/Editor/*' 2>/dev/null | sort | group gma
find "$PROJ/Assets/Firebase" "$PROJ"/Library/PackageCache/com.google.firebase.* -maxdepth 4 -name 'Firebase.*.dll' -not -path '*/Editor/*' 2>/dev/null | sort | group firebase
LIB=$PROJ/Library/ScriptAssemblies
ls "$LIB/UnityEngine.UI.dll" 2>/dev/null | group ui
ls "$LIB/Unity.InputSystem.dll" "$LIB/Unity.InputSystem.ForUI.dll" 2>/dev/null | group inputsystem
ls "$LIB/AdjustSdk.Scripts.dll" 2>/dev/null | group adjust
{ ls "$LIB/UnityEngine.TestRunner.dll" "$LIB/UnityEditor.TestRunner.dll" 2>/dev/null
  find "$PROJ/Library/PackageCache" "$C/Resources/PackageManager/BuiltInPackages" -name nunit.framework.dll -path '*unity-custom*' 2>/dev/null | head -1
} | group testrunner

COMMON="-nologo -nostdlib -langversion:9.0 -target:library -warn:4 -nowarn:1701,1702,0649"
# run() is often the end of a pipe, a subshell: a failure leaves a file instead of setting a variable.
FAILED_MARK=$OUT/.failed

# run NAME DEFINES REFS... < source files
#   REFS are reference groups or assemblies built earlier, such as android/HDC.Ads.
run() {
  local name=$1 defines=$2 ref
  shift 2
  for ref in "$@"; do
    if [ -f "$G/$ref" ] && [ ! -s "$G/$ref" ]; then
      printf '%-8s%s (no %s in the project)\n' skip "$name" "$ref"; cat > /dev/null; return
    fi
    if [ ! -f "$G/$ref" ] && [ ! -f "$OUT/$ref.dll" ]; then
      printf '%-8s%s (%s did not build)\n' skip "$name" "$ref"; cat > /dev/null; return
    fi
  done
  mkdir -p "$(dirname "$OUT/$name")"
  {
    echo "$COMMON"
    echo "-define:$DEFINES;$defines"
    echo "-out:\"$OUT/$name.dll\""
    for ref in "$@"; do
      if [ -f "$G/$ref" ]; then cat "$G/$ref"; else echo "-r:\"$OUT/$ref.dll\""; fi
    done
    sed 's/.*/"&"/'
  } > "$OUT/$name.rsp"
  local log
  log=$("$DOTNET" "$CSC" -noconfig @"$OUT/$name.rsp" 2>&1 | grep -v '^$')
  if [ -f "$OUT/$name.dll" ]; then printf '%-8s%s\n' ok "$name"; else printf '%-8s%s\n' FAILED "$name"; : > "$FAILED_MARK"; fi
  [ -n "$log" ] && echo "$log" | sed "s|$SRC/||; s/^/        /" | head -20
}
src() { find "$@" -name '*.cs' | sort; }
runtime() { src "$SRC/Runtime" | grep -v -e '/Runtime/Firebase/' -e '/Runtime/Settings/'; }
debug() { src "$SRC/Debug" | grep -v '/Debug/Editor/'; }

src "$SRC/Runtime/Settings" | run common/HDC.Ads.Settings "UNITY_ANDROID" bcl engine
for setup in editor ios android nofirebase; do
  # E: the Editor's references, FB: Firebase and the assembly built over it, both empty where they do not apply.
  # A: the Adjust SDK, empty where the setup has none.
  case $setup in
    editor) D="UNITY_EDITOR;UNITY_EDITOR_OSX;UNITY_IOS;HDC_ADS;HDC_FIREBASE;HDC_ADJUST;ENABLE_LEGACY_INPUT_MANAGER"; E=editor; A=adjust;;
    ios) D="UNITY_IOS;ENABLE_IL2CPP;HDC_ADS;HDC_FIREBASE;HDC_ADJUST;ENABLE_LEGACY_INPUT_MANAGER"; E=; A=adjust;;
    android) D="UNITY_ANDROID;HDC_ADS;HDC_FIREBASE;HDC_ADJUST;ENABLE_LEGACY_INPUT_MANAGER"; E=; A=adjust;;
    nofirebase) D="UNITY_EDITOR;UNITY_EDITOR_OSX;UNITY_ANDROID;HDC_ADS;ENABLE_LEGACY_INPUT_MANAGER"; E=editor; A=;;
  esac
  if [ -n "$A" ] && [ ! -s "$G/adjust" ]; then A=; D=${D/;HDC_ADJUST/}; fi
  FB=
  runtime | run $setup/HDC.Ads "$D" bcl engine $E gma common/HDC.Ads.Settings
  if [ $setup != nofirebase ]; then
    src "$SRC/Runtime/Firebase" | run $setup/HDC.Ads.Firebase "$D" bcl engine $E gma firebase $setup/HDC.Ads common/HDC.Ads.Settings
    FB="firebase $setup/HDC.Ads.Firebase"
  fi
  src "$SRC/Setup" | run $setup/HDC.Ads.Setup "$D" bcl engine $E $setup/HDC.Ads common/HDC.Ads.Settings $FB
  src "$SRC/Adjust" | run $setup/HDC.Ads.Adjust "$D" bcl engine $E $A $setup/HDC.Ads
  debug | run $setup/HDC.Ads.Debug "$D;HDC_WEB_REQUEST" bcl engine $E ui $setup/HDC.Ads common/HDC.Ads.Settings $setup/HDC.Ads.Setup $setup/HDC.Ads.Adjust
  if [ -n "$E" ]; then
    src "$SRC/Tests/Editor" | run $setup/HDC.Ads.Tests "$D;UNITY_INCLUDE_TESTS" bcl engine editor ui testrunner $setup/HDC.Ads common/HDC.Ads.Settings $setup/HDC.Ads.Debug $setup/HDC.Ads.Adjust $FB
  fi
done
debug | run inputsystem/HDC.Ads.Debug "UNITY_ANDROID;HDC_ADS;HDC_FIREBASE;HDC_INPUT_SYSTEM;ENABLE_INPUT_SYSTEM" bcl engine ui inputsystem android/HDC.Ads common/HDC.Ads.Settings android/HDC.Ads.Setup android/HDC.Ads.Adjust
src "$SRC/Setup" | run off/HDC.Ads.Setup "UNITY_ANDROID;ENABLE_LEGACY_INPUT_MANAGER" bcl engine common/HDC.Ads.Settings
src "$SRC/Adjust" | run off/HDC.Ads.Adjust "UNITY_ANDROID;ENABLE_LEGACY_INPUT_MANAGER" bcl engine
src "$SRC/Adjust" | run offadjust/HDC.Ads.Adjust "UNITY_IOS;ENABLE_IL2CPP;HDC_ADJUST;ENABLE_LEGACY_INPUT_MANAGER" bcl engine adjust
src "$SRC/Debug/Editor" | run editor/HDC.Ads.Debug.Editor "UNITY_EDITOR;UNITY_EDITOR_OSX;UNITY_IOS;HDC_ADS" bcl engine editor ui editor/HDC.Ads.Debug editor/HDC.Ads
src "$SRC/Editor" | run editorios/HDC.Ads.Editor "UNITY_EDITOR;UNITY_EDITOR_OSX;UNITY_IOS" bcl engine editor xcode common/HDC.Ads.Settings editor/HDC.Ads.Adjust
src "$SRC/Editor" | run editorandroid/HDC.Ads.Editor "UNITY_EDITOR;UNITY_EDITOR_OSX;UNITY_ANDROID" bcl engine editor common/HDC.Ads.Settings editor/HDC.Ads.Adjust
for setup in editor android; do
  case $setup in
    editor) D="UNITY_EDITOR;UNITY_EDITOR_OSX;UNITY_IOS;HDC_ADS;HDC_FIREBASE;HDC_ADJUST"; E=editor;;
    android) D="UNITY_ANDROID;HDC_ADS;HDC_FIREBASE;HDC_ADJUST"; E=;;
  esac
  src "$SRC/Tests/Scenes" | run $setup/HDC.Ads.TestScenes "$D" bcl engine $E ui $setup/HDC.Ads common/HDC.Ads.Settings $setup/HDC.Ads.Setup $setup/HDC.Ads.Adjust $setup/HDC.Ads.Debug
done
[ ! -f "$FAILED_MARK" ]
