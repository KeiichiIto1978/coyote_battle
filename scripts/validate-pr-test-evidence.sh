#!/usr/bin/env bash
set -euo pipefail

pr_body="${PR_BODY:-}"

missing=0
printf '%s\n' "$pr_body" | grep -Eq '^対象コミット: [0-9a-fA-F]{40}\r?$' || missing=1
printf '%s\n' "$pr_body" | grep -Eq '^実行コマンド: .+\r?$' || missing=1
printf '%s\n' "$pr_body" | grep -Eq '^結果: 成功\r?$' || missing=1
printf '%s\n' "$pr_body" | grep -Eq '^成功件数: [1-9][0-9]*\r?$' || missing=1

if [[ "$missing" -ne 0 ]]; then
    printf 'ERROR: PR本文に対象コミット、実行コマンド、成功結果、成功件数を記載してください。\n' >&2
    exit 1
fi

printf 'ローカルEditModeテスト証跡を確認しました。\n'
