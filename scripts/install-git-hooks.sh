#!/usr/bin/env bash
set -euo pipefail

repository_root="${1:-$(git rev-parse --show-toplevel)}"
expected_hooks_path='.githooks'
current_hooks_path="$(git -C "$repository_root" config --local --get core.hooksPath || true)"

if [[ -n "$current_hooks_path" && "$current_hooks_path" != "$expected_hooks_path" ]]; then
    printf 'ERROR: core.hooksPathには既に別の値が設定されています: %s\n' "$current_hooks_path" >&2
    exit 1
fi

git -C "$repository_root" config --local core.hooksPath "$expected_hooks_path"
printf 'Git hooksを有効化しました: %s\n' "$expected_hooks_path"
