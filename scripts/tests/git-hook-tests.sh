#!/usr/bin/env bash
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
installer="$repository_root/scripts/install-git-hooks.sh"
temporary_root="$(mktemp -d)"
trap 'rm -rf "$temporary_root"' EXIT

passed=0
failed=0

# 期待する終了コードとhooksPathを入力し、設定変更の安全性を検証する。
assert_case() {
    local name="$1"
    local expected_exit="$2"
    local expected_value="$3"
    local repository="$4"
    local actual_exit=0

    bash "$installer" "$repository" >/dev/null 2>&1 || actual_exit=$?
    actual_value="$(git -C "$repository" config --local --get core.hooksPath || true)"

    if [[ "$actual_exit" == "$expected_exit" && "$actual_value" == "$expected_value" ]]; then
        printf 'PASS: %s\n' "$name"
        passed=$((passed + 1))
    else
        printf 'FAIL: %s (exit=%s, hooksPath=%s)\n' "$name" "$actual_exit" "$actual_value"
        failed=$((failed + 1))
    fi
}

repository="$temporary_root/unconfigured"
git init -q "$repository"
# 未設定と同値設定で、安全かつ冪等に導入できることを確認する。
assert_case '未設定なら導入' 0 '.githooks' "$repository"
assert_case '同じ設定なら冪等' 0 '.githooks' "$repository"

repository="$temporary_root/configured"
git init -q "$repository"
git -C "$repository" config --local core.hooksPath custom-hooks
# 利用者の既存hook構成を無断上書きしないことを確認する。
assert_case '異なる設定を保護' 1 'custom-hooks' "$repository"

printf '\nResult: %s passed, %s failed\n' "$passed" "$failed"
[[ "$failed" -eq 0 ]]
