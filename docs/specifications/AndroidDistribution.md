# Android実機確認・限定配布仕様

## 目的

Version 1をAndroid実機へ安全かつ再現可能に導入し、主要ゲームフローを完走できることを確認する。一般公開は行わず、初版は信頼できる家族、友人、知人へ検証用APKを直接共有する。

## 対応環境

- Unity Editor: `6000.3.21f1`
- 表示名: `Coyote Battle`
- Application ID: `com.keiichiito.coyotebattle`
- Minimum API Level: 25（Android 7.1）
- Target API Level: 35
- CPU ABI: ARM64（`arm64-v8a`）のみ
- 画面: Landscape Left固定。縦向きと実行中の自動回転は無効
- 初版versionName: `1.0`
- 初版versionCode: `1`

Unity 6.3 LTSのAndroid Player最低要件はAndroid 7.1（API 25）である。API 24以下、ARMv7、x86、x86_64はVersion 1のサポート対象外とする。

## 成果物

### 本人端末での確認

`scripts/Build-AndroidDevelopment.ps1`が生成する`Builds/Android/CoyoteBattle-development.apk`を使用する。Development APKはデバッグと本人端末のスモーク確認専用であり、限定配布物として扱わない。

### 限定配布

`release`更新時のUnity Build Automation（UBA）設定`Release Android`が生成したAPKを使用する。初版は既存Release CI仕様どおり、自動生成debug keystoreで署名された検証用APKとする。

- Google Play、公開GitHub Release、誰でも取得できるURLへ掲載しない。
- Google Driveの「制限付き」共有で、信頼できる相手のGoogleアカウントだけへ直接共有する。共有先情報はリポジトリへ記録しない。
- AABとGoogle Play内部テストは使用しない。
- 本番用custom keystoreと長期更新用署名は別途設計する。

## ローカルビルド

Android Toolsは非ASCIIプロジェクトパスを扱えないため、既存のREADME手順で未使用ドライブへ一時割り当てしてDevelopment APKを生成する。追跡対象に未コミット変更がある場合はビルドしない。ビルドは既存APKを削除してから実行し、終了コード、更新日時、非ゼロサイズを検証する。APKと同時にcommit SHA、サイズ、SHA-256を含む`.build.json`を生成する。

## ADBによる実機導入

### 事前準備

1. 端末の開発者向けオプションとUSBデバッグを有効にする。
2. USB接続後、端末に表示されるPCのデバッグ許可を承認する。
3. 対象端末以外のAndroid端末とエミュレーターを切断する。
4. 新しいDevelopment APKを生成する。

### 安全条件

`scripts/Install-AndroidDevelopment.ps1`はUnity同梱ADBを利用する。次をすべて満たす場合だけ`adb install -r`を実行する。

- ADBが列挙した端末がちょうど1台である。
- 端末状態が`device`であり、`unauthorized`または`offline`ではない。
- API Levelが25以上である。
- ABIが`arm64-v8a`である。
- APKが存在し、サイズが1 byte以上である。
- `.build.json`のcommit SHAが現在commit、サイズとSHA-256がAPKに一致し、追跡対象に未コミット変更がない。
- APK内のApplication ID、version、Minimum／Target API、ABIが対応環境の値と一致する。

端末0台、複数台、認証未完了、APK情報不一致、非対応API／ABIでは端末を変更せず終了する。ADB処理が既定60秒以内に完了しない場合も失敗とする。インストール失敗時も自動アンインストールしない。アンインストールはアプリデータを削除するため、必要性を確認した利用者だけが明示的に実施する。

### 証跡

インストール成功後、アプリを起動して`TestResults/AndroidDeviceEvidence.json`へ次を記録する。

- APKファイル名、サイズ、SHA-256
- 対象commitの40文字SHA
- インストール済みversionNameとversionCode
- 端末メーカー、モデル、Android版、API Level、ABI
- UTC記録時刻

ADB serial、氏名、メールアドレス、端末認証コードは表示・保存しない。`TestResults/`はGit管理対象外とし、必要な結果だけをIssueまたはPRへ転記する。

## 実機スモーク確認

1. 新規起動でTitleが横向きかつSafe Area内に表示される。
2. Title BGM、Battle BGM、BGM ON/OFF、音量0からの復帰が動作する。
3. Rulesを最終項目までスクロールしTitleへ戻れる。
4. 数字キーボードで1文字と10文字を入力し、フォーカス中、無効状態、キーボードを閉じた後も文字全体を判読して宣言できる。
5. NPCの連続手番とユーザー手番で、NPC行、中央の宣言表示、下部のユーザー情報・操作が重ならない。
6. コヨーテ、ラウンド継続、脱落、最終勝敗まで1ゲーム完走できる。
7. 再戦とTitle復帰が動作する。
8. ホーム移動でBGMが停止し、復帰後に設定どおり再開する。
9. 終了・再起動でTitleへ戻り、BGM設定だけが保持される。
10. 機内モードでも主要ゲームフローを完走できる。
11. ノッチ、角丸、ナビゲーション領域へ主要操作が隠れない。

端末モデル、Android版、API Level、ABI、実施日、各項目の結果をIssueまたはPRへ記録する。ADB serialと利用者個人情報は記録しない。

## 更新とロールバック

- 同じApplication IDを上書き更新する場合は、同じ署名を使いversionCodeを増やす。
- 同じversionCode、低いversionCode、異なる署名のAPKは上書きできない場合がある。
- 署名を変更する場合はアンインストールが必要になり、BGM設定を含むローカルデータが削除される。
- 初版のdebug署名APKは長期更新を保証しない。この制約を共有相手へ明示する。
- 問題があるAPKは再共有せず、修正を`main`へ反映して品質ゲート後に新しい`release`候補を作る。

## 限定配布時の確認情報

配布前に次をそろえ、不一致または空値があれば配布しない。

- UBAの対象commit SHAと配布予定commitの一致
- UBAのEditMode成功件数とBuild成功結果
- APK Artifact名、非ゼロサイズ、SHA-256
- versionName、versionCode、Application ID
- Android 7.1以上、ARM64のみという対応条件
- 「不明なアプリのインストール」許可が必要なこと
- 手動更新、署名変更時の再インストールとデータ消失の制約

### 送信者の共有手順

1. UBAから対象commitのAPKを取得し、上記確認情報を検証する。
2. Google DriveへAPKをアップロードし、「一般的なアクセス」を「制限付き」のままにする。
3. 受取人のGoogleアカウントだけを閲覧者として追加する。メールアドレスをIssue、PR、Gitへ転記しない。
4. APKのファイル名、SHA-256、version、対応環境、手動更新とデータ消失の制約を1対1の連絡で伝える。
5. 確認終了後はDriveの共有設定から受取人を削除し、不要になったAPKを削除する。

### 受取人のインストール手順

1. 自分宛てに制限共有されたGoogle Drive上のAPKだけをダウンロードする。
2. 送信者から伝えられたファイル名とversionを確認する。PCで確認する場合はPowerShellの`Get-FileHash <APKのパス> -Algorithm SHA256`で通知されたSHA-256と照合する。
3. Androidの「設定」から、APKを開くために使用するGoogle Driveまたはファイル管理アプリだけへ「不明なアプリのインストール」を一時的に許可する。
4. APKを開いてインストールし、`Coyote Battle`を起動する。更新時はアンインストールせず、そのまま上書きする。
5. インストール後、手順3の許可をOFFへ戻す。

署名不一致やversionCode低下で更新できない場合は、自己判断でアンインストールしない。送信者へ連絡し、BGM設定を含むローカルデータ消失を了承した場合だけ再インストールする。

## Secretと個人情報

- `*.jks`、`*.keystore`、password、PATをGit、Issue、PR、ログへ含めない。
- APKのSHA-256と署名証明書fingerprintはSecretではないが、APK本体は公開しない。
- 配布先一覧とGoogleアカウントはリポジトリで管理しない。
- ADBコマンドの生出力を証跡へ保存せず、serialを除いた許可項目だけを構造化して記録する。

## 自動検証

- EditModeでApplication ID、API Level、ARM64、version、画面向き、Android Development APK指定を検証する。
- PowerShellテストでAPK情報、ビルド証跡、timeout、端末0台／1台／複数台／`unauthorized`／`offline`、API 25境界、ARM64境界、証跡からのserial除外を検証する。
- GitHub ActionsでPowerShellの安全判定テストを実行する。
- 全EditMode、全PlayMode、Android Developmentビルドをマージ前に実行する。

## 未決事項

本番署名、Google Play配布、AAB、長期更新方式はVersion 1の初版検証後に別Issueで決定する。
