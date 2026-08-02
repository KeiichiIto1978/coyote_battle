#!/usr/bin/env bash
set -euo pipefail

repository_root="${QUALITY_GATE_ROOT:-$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)}"
mode="${1:-all}"

# 入力した理由を標準エラーへ出力し、呼び出し元へ失敗を返す。
fail() {
    printf 'ERROR: %s\n' "$1" >&2
    return 1
}

# リポジトリ配下から生成物を除外し、検査対象をNUL区切りで返す。
collect_files() {
    find "$repository_root" \
        -type d \( -name .git -o -name Library -o -name Logs -o -name Temp -o -name TestResults \) -prune -o \
        -type f -print0
}

# checkまたはwriteを受け取り、固定版CSharpierの終了コードを返す。
run_format() {
    local operation="$1"
    command -v dotnet >/dev/null 2>&1 || { fail 'dotnetが見つかりません。'; return 1; }
    (
        cd "$repository_root"
        dotnet tool restore
        if [[ "$operation" == 'check' ]]; then
            dotnet csharpier check Assets
        else
            dotnet csharpier format Assets
        fi
    )
}

# JSONファイルを受け取り、利用可能な標準ランタイムで構文を検証する。
validate_json() {
    local path="$1"
    if command -v python3 >/dev/null 2>&1; then
        python3 -m json.tool "$path" >/dev/null
    elif command -v python >/dev/null 2>&1; then
        python -m json.tool "$path" >/dev/null
    elif command -v pwsh >/dev/null 2>&1; then
        pwsh -NoProfile -Command "Get-Content -Raw -LiteralPath \"$path\" | ConvertFrom-Json | Out-Null"
    elif command -v powershell.exe >/dev/null 2>&1; then
        local windows_path
        windows_path="$(cygpath -w "$path")"
        powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Get-Content -Raw -LiteralPath \"$windows_path\" | ConvertFrom-Json | Out-Null"
    else
        fail 'JSON検証に必要なPythonまたはPowerShellが見つかりません。'
    fi
}

# リポジトリ規約と固定された外部linterを実行し、違反の有無を返す。
run_policy_lint() {
    local lint_failed=0
    local path relative line_count extension

    while IFS= read -r -d '' path; do
        relative="${path#"$repository_root"/}"
        extension="${path##*.}"

        case "$extension" in
            cs)
                line_count="$(awk 'END { print NR }' "$path")"
                if ((line_count > 500)); then
                    printf 'ERROR: %s は500行を超えています（%s行）。\n' "$relative" "$line_count" >&2
                    lint_failed=1
                fi
                ;;
            md)
                if [[ "$relative" == 'AGENTS.md' || "$relative" == */SKILL.md ]]; then
                    line_count="$(awk 'END { print NR }' "$path")"
                    if ((line_count > 200)); then
                        printf 'ERROR: %s は200行を超えています（%s行）。\n' "$relative" "$line_count" >&2
                        lint_failed=1
                    fi
                fi
                ;;
        esac

        case "$extension" in
            cs|md|json|asmdef|sh|ps1|yml|yaml)
                if grep -nI '[[:blank:]]$' "$path" >/dev/null; then
                    printf 'ERROR: %s に末尾空白があります。\n' "$relative" >&2
                    lint_failed=1
                fi
                if command -v iconv >/dev/null 2>&1 && ! iconv -f UTF-8 -t UTF-8 "$path" >/dev/null 2>&1; then
                    printf 'ERROR: %s は有効なUTF-8ではありません。\n' "$relative" >&2
                    lint_failed=1
                fi
                ;;
        esac

        case "$extension" in
            json|asmdef)
                if ! validate_json "$path"; then
                    printf 'ERROR: %s はJSONとして不正です。\n' "$relative" >&2
                    lint_failed=1
                fi
                ;;
        esac
    done < <(collect_files)

    if [[ "${QUALITY_GATE_SKIP_EXTERNAL_LINTERS:-0}" != '1' ]]; then
        if command -v shellcheck >/dev/null 2>&1; then
            mapfile -d '' shell_files < <(find "$repository_root" -type f -name '*.sh' -print0)
            ((${#shell_files[@]} == 0)) || shellcheck "${shell_files[@]}" || lint_failed=1
        else
            printf 'ERROR: ShellCheckが見つかりません。\n' >&2
            lint_failed=1
        fi

        if command -v pwsh >/dev/null 2>&1; then
            pwsh -NoProfile -Command "\$results = Invoke-ScriptAnalyzer -Path '$repository_root/scripts' -Recurse -Severity Warning,Error; if (\$results) { \$results; exit 1 }" || lint_failed=1
        elif command -v powershell.exe >/dev/null 2>&1; then
            local windows_scripts_path
            windows_scripts_path="$(cygpath -w "$repository_root/scripts")"
            powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "\$results = Invoke-ScriptAnalyzer -Path '$windows_scripts_path' -Recurse -Severity Warning,Error; if (\$results) { \$results; exit 1 }" || lint_failed=1
        else
            printf 'ERROR: PSScriptAnalyzerを実行できるPowerShellが見つかりません。\n' >&2
            lint_failed=1
        fi
    fi

    [[ "$lint_failed" -eq 0 ]] || return 1
    printf 'Lint成功\n'
}

# OSに適したPowerShellから既存のUnityテストを起動し、その結果を返す。
run_editmode() {
    if command -v powershell.exe >/dev/null 2>&1; then
        powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$repository_root/scripts/Test-UnityEditMode.ps1"
    elif command -v pwsh >/dev/null 2>&1; then
        pwsh -NoProfile -File "$repository_root/scripts/Test-UnityEditMode.ps1"
    else
        fail 'ローカルEditModeテストにはPowerShellが必要です。CIではGameCIを使用します。'
    fi
}

case "$mode" in
    format-check) run_format check ;;
    format-write) run_format write ;;
    lint) run_policy_lint ;;
    editmode) run_editmode ;;
    all)
        run_format check
        run_policy_lint
        run_editmode
        ;;
    *)
        printf 'ERROR: 未対応のモードです: %s\n' "$mode" >&2
        exit 2
        ;;
esac
