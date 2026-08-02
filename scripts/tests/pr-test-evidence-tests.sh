#!/usr/bin/env bash
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
validator="$repository_root/scripts/validate-pr-test-evidence.sh"
passed=0
failed=0

# PR本文と期待終了コードを入力し、ローカルテスト証跡の必須条件を検証する。
assert_case() {
    local name="$1"
    local body="$2"
    local expected_exit="$3"
    local actual_exit=0

    PR_BODY="$body" bash "$validator" >/dev/null 2>&1 || actual_exit=$?
    if [[ "$actual_exit" == "$expected_exit" ]]; then
        printf 'PASS: %s\n' "$name"
        passed=$((passed + 1))
    else
        printf 'FAIL: %s (exit=%s)\n' "$name" "$actual_exit"
        failed=$((failed + 1))
    fi
}

valid_body=$'## ローカルEditModeテスト証跡\n対象コミット: 0123456789abcdef0123456789abcdef01234567\n実行コマンド: bash scripts/quality-gate.sh editmode\n結果: 成功\n成功件数: 4'

# 必須4項目が揃った成功証跡だけを受理する。
assert_case '正常な成功証跡' "$valid_body" 0
assert_case '対象コミットなし' $'実行コマンド: test\n結果: 成功\n成功件数: 4' 1
assert_case '失敗結果' $'対象コミット: 0123456789abcdef0123456789abcdef01234567\n実行コマンド: test\n結果: 失敗\n成功件数: 0' 1

printf '\nResult: %s passed, %s failed\n' "$passed" "$failed"
[[ "$failed" -eq 0 ]]
