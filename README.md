# Coyote Battle

カードゲーム「コヨーテ」の推理・ブラフ・リスク判断を題材にした、Android 向けの一人用カードゲームです。
プレイヤー1名と NPC 4名の計5名で対戦します。

## プロジェクトの目的

本プロジェクトではゲームの完成に加え、Unity、Git、Codex を使った共同開発を通して、ゲームロジック設計、NPC 思考、テスト駆動開発、CI、Android アプリ配布を実践することを目的とします。

詳細なゲーム仕様は [企画書](docs/proposal.md) を参照してください。

## 初期リリースの範囲

- プレイヤー1名と NPC 4名による対戦
- カード配布と表示・非表示の制御
- 数字宣言とコヨーテ宣言
- カード合計値の計算とラウンド敗者の判定
- ライフ、脱落、次ラウンド、勝敗の管理
- 基本的な特殊カード
- 複数タイプの NPC 思考

## 技術方針

- Unity / C# による Android アプリ開発
- UI とゲームロジックを分離
- NUnit と Unity Test Framework によるテスト駆動開発
- `.editorconfig` による基本的なコードスタイルの統一
- Git標準pre-push hookとGitHub Actionsによる品質ゲート

## Unity Editor

- 使用バージョン: Unity `6000.3.21f1`（Unity 6.3 LTS）
- Unity HubではAndroid Build Support、Android SDK & NDK Tools、OpenJDKを追加する
- Unityは`ProjectSettings/ProjectVersion.txt`からバージョンを自動検出する
- 更新前の`6000.1.0f1`は、更新PRとUBA検証が完了するまでロールバック用に残す

Editor更新とロールバックの詳細は[Unity Editor運用仕様](docs/specifications/UnityEditor.md)を参照してください。

## ディレクトリ構成

```text
Assets/
  CoyoteBattle/
    Scenes/
      Bootstrap.unity  Androidビルドの起点となる最小シーン
    Scripts/
      Domain/       UIやUnityのライフサイクルに依存しないゲームロジック
      Application/  ゲーム進行とユースケース（今後追加）
      Presentation/ 画面表示と入力（今後追加）
    Tests/
      EditMode/     高速に実行できるロジックテスト
Packages/           Unity Package Manager の設定
ProjectSettings/    Unity プロジェクト設定
docs/               企画・設計ドキュメント
scripts/            テストやビルドの自動化スクリプト
```

## 品質ゲート

Git for WindowsのBashまたはPOSIX shellから、formatter、lint、EditModeテストを個別または一括実行できます。

```bash
bash scripts/quality-gate.sh format-check
bash scripts/quality-gate.sh lint
bash scripts/quality-gate.sh editmode
bash scripts/quality-gate.sh all
```

初回clone後に次を実行すると、push前に`all`が自動実行されます。

事前にShellCheck 0.11.0とPSScriptAnalyzer 1.24.0を導入してください。CSharpier 1.3.0は共通コマンドがリポジトリのtool manifestから復元します。

```bash
bash scripts/install-git-hooks.sh
```

整形を適用する場合は`bash scripts/quality-gate.sh format-write`を使用します。CI、Unityライセンス、branch protectionの詳細は[品質ゲート仕様](docs/specifications/QualityGates.md)を参照してください。

## リリースCI

通常のPull Requestでは、GitHub Actionsによるformatter・lintと、ローカルpre-pushによるEditModeテストを実行します。クラウドでのUnityテストとAndroid APKビルドは、保護された`release`ブランチが更新された場合だけUnity Build Automationで実行します。

リリース候補は`main`から`release`へのPull Requestで作成します。`release`への直接push、force push、削除は禁止し、`Quality Gates`を必須チェックとします。Unity Build Automationが成功した成果物だけをリリース候補として扱います。

Build Automationの設定値、無料枠、失敗時の復旧方法は[リリースCI仕様](docs/specifications/ReleaseCi.md)を参照してください。

Androidビルド対象には`Assets/CoyoteBattle/Scenes/Bootstrap.unity`を登録しています。ゲーム画面は今後この起点から構成し、Build Settingsから有効なシーンをすべて外さないでください。

## EditModeテスト

### 前提条件

- `ProjectSettings/ProjectVersion.txt` に記載されたUnityがインストールされていること
- 対象プロジェクトをUnity Editorで開いていないこと
- Windows PowerShell 5.1以降を利用できること

### 実行方法

リポジトリのルートで次のコマンドを実行します。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-UnityEditMode.ps1
```

スクリプトは `ProjectSettings/ProjectVersion.txt` に記載されたUnityをUnity Hubの標準インストール先から検出します。別の場所にUnityをインストールしている場合は、次のように指定できます。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-UnityEditMode.ps1 `
  -UnityPath "D:\Unity\Editor\Unity.exe"
```

タイムアウトは既定で300秒です。変更する場合は秒数を指定します。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-UnityEditMode.ps1 `
  -TimeoutSeconds 600
```

### 実行結果

スクリプトは二重起動、Unityの終了コード、タイムアウト、テスト結果の生成、テスト件数、失敗件数を検証します。すべてのテストが成功した場合は終了コード`0`、問題がある場合は`1`を返すため、ローカル実行とCIで同じ判定を利用できます。

結果とログは次の場所に出力され、Gitの管理対象には含まれません。

- `TestResults/EditMode.xml`: NUnit形式のテスト結果
- `TestResults/EditMode.log`: Unityの実行ログ

## Android Developmentビルド

Unity 6.3のAndroid Toolsは、プロジェクトパスに日本語などの非ASCII文字が含まれるとビルドできません。現在の配置では、一時的にASCIIのみのドライブへ割り当てて実行します。

```powershell
$createdMapping = $false
if (subst | Select-String '^U:') {
  throw 'U: は既に使用されています。未使用のドライブ文字へ置き換えてください。'
}
try {
  subst U: "$PWD"
  if ($LASTEXITCODE -ne 0) {
    throw '一時ドライブの割り当てに失敗しました。'
  }
  $createdMapping = $true
  powershell -NoProfile -ExecutionPolicy Bypass `
    -File U:\scripts\Build-AndroidDevelopment.ps1 `
    -ProjectPath U:\
} finally {
  if ($createdMapping) {
    subst U: /D
  }
}
```

スクリプトはEditorとAndroid SDK・NDK・JDKを検出し、追跡対象に未コミット変更がないことを確認します。以前のAPKを削除してから、Development APKの終了コード、更新日時、ファイルサイズを検証し、commit SHA、サイズ、SHA-256を`.build.json`へ記録します。ASCIIだけで構成された空白入りパスにも対応します。

成果物は`Builds/Android/CoyoteBattle-development.apk`、ログは`Logs/AndroidDevelopmentBuild.log`へ出力され、Gitの管理対象には含まれません。

## Android実機へのインストール

Android 7.1（API 25）以上のARM64端末でUSBデバッグを有効にし、対象端末だけを接続します。非ASCIIパスの問題を避けるため、ビルドと同じ一時ドライブ上で実行します。

```powershell
$createdMapping = $false
if (subst | Select-String '^U:') {
  throw 'U: は既に使用されています。未使用のドライブ文字へ置き換えてください。'
}
try {
  subst U: "$PWD"
  if ($LASTEXITCODE -ne 0) {
    throw '一時ドライブの割り当てに失敗しました。'
  }
  $createdMapping = $true
  powershell -NoProfile -ExecutionPolicy Bypass `
    -File U:\scripts\Install-AndroidDevelopment.ps1 `
    -ProjectPath U:\
} finally {
  if ($createdMapping) {
    subst U: /D
  }
}
```

スクリプトはビルド証跡とAPK内のApplication ID、version、API、ABIを先に検証します。その後、端末が1台で認証済み、API 25以上、`arm64-v8a`の場合だけAPKを上書きインストールし、アプリを起動します。端末0台、複数台、認証未完了、APK不一致、非対応端末、ADB timeoutではインストールしません。失敗時も自動アンインストールは行いません。

証跡は`TestResults/AndroidDeviceEvidence.json`へ出力されます。ADB serialと個人情報は記録されません。実機スモーク項目と限定配布手順は[Android実機確認・限定配布仕様](docs/specifications/AndroidDistribution.md)を参照してください。

## 開発状況

Version 1のゲーム本編、BGM、ルール説明画面まで実装済みです。Android実機の完走確認と初版限定配布の検証を行います。
