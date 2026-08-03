# Unity Editor運用仕様

## 使用バージョン

- 現行EditorはUnity 6.3 LTSの`6000.3.21f1`とする。
- `ProjectSettings/ProjectVersion.txt`を、ローカルとUnity Build Automationが参照する一次情報とする。
- Unity HubではAndroid Build Support、Android SDK & NDK Tools、OpenJDKを導入する。
- UBAでは自動検出を使い、完全一致しないEditorへのフォールバックを無効にする。

Unity 6.3 LTSは、Unity公式の長期サポート対象かつUBAのSupported対象であることを選定理由とする。

## 更新手順

1. 既存Editorを残したまま、Unity Hubで更新先Editorと必要モジュールを導入する。
2. `origin/main`からEditor更新専用ブランチを作成する。
3. 更新前のコミットSHA、Editor版、EditMode件数を記録する。
4. 更新先EditorでEditModeテストを実行し、プロジェクトを変換する。
5. ProjectSettings、Package、アセットの差分を確認する。
6. formatter、lint、EditMode、Android Developmentビルドを実行する。
7. main反映後、mainからreleaseへPRを作り、UBAのテストとAPK生成を確認する。

Editor更新に不要なPackage更新、`Library/`、`Logs/`、`TestResults/`、`Builds/`、個人設定はコミットしない。

## ローカル検証

- EditModeテストの終了コードが0で、テストが1件以上実行され、失敗が0件であること。
- コンパイルエラーと未解決のPackageエラーがないこと。
- `Assets/CoyoteBattle/Scenes/Bootstrap.unity`が有効なビルド対象として読み込めること。
- Android Development APKが生成され、ファイルサイズが0より大きいこと。

Android Toolsは非ASCII文字を含むプロジェクトパスを拒否する。該当する環境では、READMEの手順でworktreeを一時的なASCIIドライブへ割り当てる。割り当てはビルド後に必ず解除する。

## UBA検証

- 対象ブランチは`release`とする。
- Editor `6000.3.21f1`を完全一致で使用する。
- 対象commit SHA、EditMode結果、APK Artifact、billable build timeを記録する。
- 無料枠不足または課金要求が表示された場合は、課金せず検証を保留する。

## ロールバック

- 更新PRのマージ前はブランチを破棄し、旧Editorで元コミットを開く。
- マージ後は`ProjectVersion.txt`だけでなく、Editor変換による差分一式をrevert PRで戻す。
- 新Editorで保存したアセットを旧Editorで上書きしない。
- 更新PRとUBA検証が完了するまでは旧`6000.1.0f1`をアンインストールしない。
