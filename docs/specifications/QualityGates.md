# 品質ゲート仕様

## 目的

push前とPull Request時に同じ検査契約を使い、整形漏れ、規約違反、EditModeテスト失敗を早期に検出する。

## 採用方式

- 共通入口は`scripts/quality-gate.sh`とし、Git for WindowsのBashとLinuxで実行する。
- pre-pushはGit標準hookを使う。Node.js依存を追加するHuskyと、追加バイナリを必要とするLefthookは採用しない。
- PowerShellはWindows版Unity Editorを起動する既存スクリプトだけに限定し、静的検査やhookの必須基盤にしない。
- CIはformatter、lint、品質ゲート自身のテスト、PR本文のローカルEditMode証跡検査だけを実行する。
- EditModeテストはpre-pushで必須とし、PRの最新コミットに対する結果をPR本文へ転記する。

## 共通コマンド

| モード | 内容 |
| --- | --- |
| `format-check` | CSharpierによる整形確認 |
| `format-write` | CSharpierによる整形 |
| `lint` | 行数、末尾空白、UTF-8、JSON、Shell、PowerShellの検査 |
| `editmode` | ローカルUnity EditModeテスト |
| `all` | 上記の確認系をfail-fastで実行 |

実行例は次のとおり。

```bash
bash scripts/quality-gate.sh lint
bash scripts/quality-gate.sh all
```

## pre-push

次のコマンドでリポジトリ管理されたhookを有効化する。

```bash
bash scripts/install-git-hooks.sh
```

既存の異なる`core.hooksPath`がある場合は上書きせず失敗する。hookはpush前に`all`を実行し、失敗時はpushを中止する。

ローカルではCSharpier 1.3.0、ShellCheck 0.11.0、PSScriptAnalyzer 1.24.0を使用する。CSharpierは共通コマンドが復元する。ShellCheckとPSScriptAnalyzerは各公式配布手順で固定版を導入してからhookを有効化する。

## GitHub Actions

Pull Requestと手動実行で`.github/workflows/quality-gates.yml`を実行する。

- 権限は`contents: read`のみ。
- 同じPull Requestの古い実行はキャンセルする。
- 品質ゲートjobは10分でtimeoutする。
- PR本文に対象コミットSHA、実行コマンド、`結果: 成功`、1件以上の成功件数がなければ失敗する。
- ActionsはフルSHAで固定し、追跡用のメジャータグをコメントに記載する。

## Unityライセンス

Unity 6ではPersonalの手動アクティベーションが公式サポート外である。GameCIの旧`unity-request-activation-file@v2`も実行時に`This action is no longer supported`として停止し、`.alf`を生成できないことを2026-08-02に確認した。このため旧方式は採用せず、DOM変更などによる制限回避も行わない。

Personal運用ではGitHub ActionsにUnityライセンスSecretを登録せず、ローカルpre-pushでEditModeテストを実行する。PR作成者は最新コミットでの結果をPRテンプレートへ転記し、CIは必須項目を機械的に検査する。

## branch protection

workflowがdefault branchへ入り、一度実行された後、branch protectionまたはrulesetで`Quality Gates`を必須チェックに指定する。個別job名ではなく集約jobだけを指定し、内部構成変更で保護設定が壊れないようにする。

## テスト方針

`scripts/tests/quality-gate-tests.sh`で正常系、異常系、500／501行、200／201行などの境界値を検証する。`scripts/tests/git-hook-tests.sh`で初回導入、再実行、既存設定保護を検証する。`scripts/tests/pr-test-evidence-tests.sh`でPR証跡の必須項目と失敗結果の拒否を検証する。
