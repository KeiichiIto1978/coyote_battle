# リリースCI仕様

## 目的

通常開発のCIコストを抑えながら、リリース候補を開発者端末とは独立した環境で再検証し、Android APKを生成する。

## ブランチ運用

- 開発の流れは`feature -> main -> release`とする。
- `release`は`main`からのPull Requestでだけ更新する。
- `release`への直接push、force push、削除は禁止する。
- `release`へのPull Requestは`Quality Gates`の成功を必須とする。
- Unity Build Automation（UBA）は`release`更新時だけ起動する。
- UBA成功前のcommitへタグを付けず、成果物を配布しない。

## Unity Build Automation設定

設定名は`Release Android`とし、次の値を使用する。

| 項目 | 設定値 |
| --- | --- |
| Platform | Android |
| Branch | `release` |
| Unity version | `ProjectSettings/ProjectVersion.txt`から自動検出 |
| Build with closest version | 無効 |
| Builder OS | Windows 11 24H2 |
| Machine | MICRO |
| Android SDK | 35 |
| Bundle ID | `com.keiichiito.coyotebattle` |
| Credentials | Auto-generated debug keystore |
| Auto-build | 有効 |
| Auto-cancel | 有効 |
| Scheduled build | 無効 |
| Unit tests | 有効 |
| EditMode tests | 有効 |
| PlayMode tests | 無効 |
| Mark build failed on test failure | 有効 |
| Android App Bundle | 無効（APKを生成） |
| Cache | Project設定のLibrary cacheを継承 |

Android Playerのビルド起点として`Assets/CoyoteBattle/Scenes/Bootstrap.unity`をBuild Settingsへ登録する。スケルトン段階の`Bootstrap`は表示やゲーム進行を持たない最小シーンとし、後続機能が起動構成を追加する。有効なビルド対象シーンが0件、または登録先が存在しない状態はEditModeテストで拒否する。

Dashboard上の設定には秘密情報を記録しない。GitHub接続用PATは`coyote_battle`だけを対象とし、`Contents: Read-only`と`Webhooks: Read and write`だけを付与する。PATの値はリポジトリ、Issue、ログへ残さない。

## 費用管理

- Unity DevOps Free Trialの無料枠だけを使用する。
- Windows MICROの無料枠は開始時点で200分である。
- 有料プランへのアップグレードや支払い方法の登録は自動で行わない。
- 無料枠の残量不足、UBA障害、認証切れの場合はリリースを保留する。
- 料金と無料枠は変更され得るため、リリースCI設定を変更する前にDashboardで再確認する。

## 成功条件と証跡

UBAのBuild詳細で次を確認する。

- 対象branchが`release`である。
- Last commitが対象の40文字commit SHAと一致する。
- EditModeテストが1件以上実行され、全件成功している。
- Build resultが成功である。
- Android APKをArtifactsから取得できる。
- Billable build timeが無料枠内である。

テスト0件、Artifactなし、commit不一致は成功扱いにしない。Build番号、commit SHA、テスト件数、結果、Artifact名を対象PRまたはリリース記録へ記載する。

## 失敗時の対応

1. Build詳細のCompact Log、Full Log、テスト結果を確認する。
2. `main`で修正し、通常の品質ゲートを通す。
3. `main`から`release`へのPull Requestを作成して修正する。
4. 緊急時も直接pushせず、revert commitをPull Requestで反映する。
5. UBAが成功するまで配布とタグ付けを保留する。

### 初回疎通で確認した失敗

Build #1ではGitHubから`release`を取得してUnityを起動できたが、Build Settingsの対象シーンが0件だったため`There were no scenes configured to build!`で失敗した。この失敗はVCS認証やUnityライセンスではなく、リポジトリ側のビルド設定不備として扱う。

再発防止として、EditModeテストで有効なシーンが1件以上あり、各シーンファイルが存在することを検証する。UBAの再実行は修正commitが`release`へ反映された後に行い、失敗した同一commitを設定変更なしで繰り返し実行しない。

## 制約

- UBAは`release`へのマージ後に起動するため、マージ自体は阻止しない。
- 初期成果物は自動生成デバッグkeystoreで署名した検証用APKであり、Google Playへ公開しない。
- 本番署名、AAB、Google Play配布は、秘密鍵の保管・権限・ローテーションを別途設計してから導入する。
- Unity 6000.1.0f1は2026-08-03時点でUBAの廃止予定警告が表示されるため、対応バージョンへの更新を別Issueで管理する。
