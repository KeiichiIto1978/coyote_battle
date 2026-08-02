#!/usr/bin/env bash
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
quality_gate="$repository_root/scripts/quality-gate.sh"
temporary_root="$(mktemp -d)"
trap 'rm -rf "$temporary_root"' EXIT

passed=0
failed=0

# 品質ゲートの終了コードと診断内容を同時に検証し、誤った理由での成功・失敗を防ぐ。
run_case() {
    local name="$1"
    local expected_exit="$2"
    local expected_message="$3"
    local fixture="$4"
    local mode="${5:-lint}"
    local output
    local actual_exit=0

    output="$(QUALITY_GATE_ROOT="$fixture" QUALITY_GATE_SKIP_EXTERNAL_LINTERS=1 bash "$quality_gate" "$mode" 2>&1)" || actual_exit=$?

    if [[ "$actual_exit" == "$expected_exit" && "$output" == *"$expected_message"* ]]; then
        printf 'PASS: %s\n' "$name"
        passed=$((passed + 1))
    else
        printf 'FAIL: %s (exit=%s)\n%s\n' "$name" "$actual_exit" "$output"
        failed=$((failed + 1))
    fi
}

# ケース名を入力として最小の正常な検査対象を生成し、そのパスを返す。
make_fixture() {
    local name="$1"
    local fixture="$temporary_root/$name"
    mkdir -p "$fixture/Assets" "$fixture/.agents/skills/example"
    printf '{"name":"valid"}\n' > "$fixture/Assets/Valid.asmdef"
    printf '%s\n' "$fixture"
}

fixture="$(make_fixture valid)"
printf 'namespace Example;\npublic sealed class Sample {}\n' > "$fixture/Assets/Sample.cs"
# 正常な最小構成が成功することを確認する。
run_case '規約準拠ファイル' 0 'Lint成功' "$fixture"

fixture="$(make_fixture csharp-500)"
for _ in $(seq 1 500); do printf '// line\n'; done > "$fixture/Assets/Boundary.cs"
# C#行数上限の境界値を確認する。
run_case 'C# 500行' 0 'Lint成功' "$fixture"

fixture="$(make_fixture csharp-501)"
for _ in $(seq 1 501); do printf '// line\n'; done > "$fixture/Assets/TooLong.cs"
run_case 'C# 501行' 1 '500行を超えています' "$fixture"

fixture="$(make_fixture context-201)"
for _ in $(seq 1 201); do printf 'rule\n'; done > "$fixture/.agents/skills/example/SKILL.md"
# AIコンテキスト上限超過を検出することを確認する。
run_case 'AIコンテキスト 201行' 1 '200行を超えています' "$fixture"

fixture="$(make_fixture trailing-space)"
printf '末尾空白あり \n' > "$fixture/README.md"
# 読み落としやすいテキスト品質違反を確認する。
run_case '末尾空白' 1 '末尾空白があります' "$fixture"

fixture="$(make_fixture invalid-json)"
printf '{invalid}\n' > "$fixture/Assets/Broken.asmdef"
# Unity asmdefを含むJSON構文違反を確認する。
run_case '不正JSON' 1 'JSONとして不正です' "$fixture"

fixture="$(make_fixture unknown-mode)"
# 呼び出し契約外のモードをusage errorとして区別する。
run_case '未知モード' 2 '未対応のモードです' "$fixture" 'unknown'

printf '\nResult: %s passed, %s failed\n' "$passed" "$failed"
[[ "$failed" -eq 0 ]]
