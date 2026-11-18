#!/usr/bin/env bash
# 任务完成通知。先落本地记录，配置了发送目标后再经 hermes 发出。
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
outbox="$root/Logs/notifications/outbox.txt"
target="${OPENMANUS_TARGET:-}"

mkdir -p "$(dirname "$outbox")"

usage() {
  echo "usage: $0 <title> [body] [kanban card id]" >&2
  echo "       $0 --drain" >&2
  exit 2
}

record() {
  local title="$1" body="$2" card="$3"
  printf '%s | %s | card=%s | %s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)" "$title" "${card:-none}" "$body" >>"$outbox"
}

send() {
  local text="$1"
  if [ -z "$target" ]; then
    echo "OPENMANUS_TARGET unset; notice kept in Logs/notifications/outbox.txt" >&2
    return 0
  fi
  hermes send --to "$target" "$text"
}

case "${1:-}" in
  "" | -h | --help) usage ;;
  --drain)
    [ -s "$outbox" ] || { echo "outbox empty"; exit 0; }
    send "$(cat "$outbox")" || exit 1
    mv "$outbox" "$outbox.sent-$(date -u +%Y%m%dT%H%M%SZ)"
    echo "drained into $target"
    ;;
  *)
    title="$1"; body="${2:-}"; card="${3:-}"
    line="$(date -u +%Y-%m-%dT%H:%M:%SZ) | $title | card=${card:-none}"
    record "$title" "$body" "$card"
    send "$line
$body"
    ;;
esac
