#!/bin/bash
# Runs HDCLib's Edit Mode tests with Unity in batch mode and prints each test's result.
#
# Usage: Tools~/run-tests.sh [project folder] [test filter]
#   project  the Unity project to run in (default: the one holding HDCLib). Unity cannot open a project that is
#            open in the Editor already: give it a copy, which `cp -c -R` makes in an instant on APFS.
#   filter   a test name, class or regex (default: every test of the library but the explicit ones, which
#            need the network: name one to run it)
# Environment:
#   UNITY_EDITOR_DIR   the Unity install, as for compile-matrix.sh
#   HDC_ACCEPT_API=1   writes the current public API into Tests/Editor/PublicApi.txt instead of comparing
#   HDC_TEST_CAPTURE   a folder to render the debug panel into while its tests run
# The results go to Logs/hdc-tests.xml in the project, Unity's log to Logs/hdc-tests.log.

SRC=$(cd "$(dirname "$0")/.." && pwd)
PROJ=${1:-$SRC}
PROJ=$(cd "$PROJ" && pwd) || exit 2
while [ "$PROJ" != / ] && [ ! -f "$PROJ/ProjectSettings/ProjectVersion.txt" ]; do PROJ=$(dirname "$PROJ"); done
[ -f "$PROJ/ProjectSettings/ProjectVersion.txt" ] || { echo "No Unity project at ${1:-$SRC}." >&2; exit 2; }
VERSION=$(sed -n 's/^m_EditorVersion: //p' "$PROJ/ProjectSettings/ProjectVersion.txt")
UNITY=${UNITY_EDITOR_DIR:-/Applications/Unity/Hub/Editor/$VERSION}
for BIN in "$UNITY/Unity.app/Contents/MacOS/Unity" "$UNITY/Editor/Unity" "$UNITY/Editor/Unity.exe"; do
  [ -x "$BIN" ] && break
done
[ -x "$BIN" ] || { echo "No Unity $VERSION at $UNITY; set UNITY_EDITOR_DIR." >&2; exit 2; }
if [ -f "$PROJ/Temp/UnityLockfile" ] && lsof "$PROJ/Temp/UnityLockfile" > /dev/null 2>&1; then
  echo "$PROJ is open in Unity. Run the tests in the Test Runner window, or here on a copy of the project." >&2
  exit 2
fi

# Unity runs explicit tests whenever a filter selects them: leave the network ones out unless named.
if [ -n "${2:-}" ]; then FILTER=(-assemblyNames HDC.Ads.Tests -testFilter "$2")
else FILTER=(-assemblyNames HDC.Ads.Tests -testCategory "!Network"); fi
RESULTS=$PROJ/Logs/hdc-tests.xml
LOG=$PROJ/Logs/hdc-tests.log
mkdir -p "$PROJ/Logs"
rm -f "$RESULTS"
echo "Running ${2:-HDC.Ads.Tests} in $PROJ with Unity $VERSION…"
"$BIN" -batchmode -projectPath "$PROJ" -runTests -testPlatform EditMode "${FILTER[@]}" \
  -testResults "$RESULTS" -logFile "$LOG"
code=$?

if [ ! -f "$RESULTS" ]; then
  echo "Unity wrote no results (exit $code). From $LOG:"
  grep -E 'error CS[0-9]+|Scripts have compiler errors|Aborting batchmode' "$LOG" | sort -u | head -20
  exit $code
fi
python3 - "$RESULTS" <<'EOF' || grep -o '<test-run [^>]*>' "$RESULTS"
import re, sys
text = open(sys.argv[1], encoding="utf-8").read()
attrs = lambda tag: dict(re.findall(r'(\w+)="([^"]*)"', tag))
run = attrs(re.search(r"<test-run [^>]*>", text).group(0))
print(f"{run['result']}: {run['passed']} passed, {run['failed']} failed, {run['skipped']} skipped of {run['total']}")
for case in re.finditer(r"<test-case ([^>]*?)(?:/>|>(.*?)</test-case>)", text, re.S):
    test = attrs(case.group(1))
    result = test.get("label") or test["result"]
    print(f"  {result:<12} {test['fullname']}  {float(test.get('duration', 0)):.1f}s")
    if test["result"] == "Failed" and case.group(2):
        message = re.search(r"<message><!\[CDATA\[(.*?)\]\]></message>", case.group(2), re.S)
        if message:
            print("               " + message.group(1).strip().replace("\n", "\n               "))
EOF
exit $code
