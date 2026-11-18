#!/usr/bin/env bash
# 把共享的 TestResults.xml 立刻复制到证据目录。
# 多个工作树共用 %LOCALLOW%\DefaultCompany\My project\TestResults.xml，
# 并行跑测试会互相覆盖；跑完立刻复制，之后引用副本而不是原文件。
#
# 用法: scripts/copy-testresults.sh <证据目录>
#   例: scripts/copy-testresults.sh Logs/verify/m04-fix-20261118
set -euo pipefail

if [ $# -lt 1 ]; then
  echo "用法: $0 <证据目录>" >&2
  exit 2
fi

dest="$1"
src="${USERPROFILE:-$HOME}/AppData/LocalLow/DefaultCompany/My project/TestResults.xml"

if [ ! -f "$src" ]; then
  echo "找不到 TestResults.xml: $src" >&2
  exit 1
fi

mkdir -p "$dest"
cp "$src" "$dest/TestResults.xml"

echo "已复制: $dest/TestResults.xml"
md5sum "$dest/TestResults.xml"
