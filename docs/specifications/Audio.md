# オーディオ

## 目的

タイトルでは落ち着いた期待感、ゲーム中は疾走感と高揚感を与える対照的なオリジナルBGMを提供する。

## 音源

- Title曲「Coyote Battle Title Theme」は96秒、75 BPM、Cメジャー系とする。打楽器を使わず、長い余韻のシンセパッドとベル風の旋律で、静かで明るく余白のある音楽にする。
- プレイ曲「Coyote Battle Theme」は96秒、120 BPM、Dマイナー・ペンタトニックを中心とする。ピアノ風の減衰音、シンセパッド、ベース、打楽器による疾走感のある和風ファンタジーとする。
- 2曲ともステレオ、22,050 HzのWAVとし、先頭と末尾のPCM値を0にそろえ、クリックノイズや目立つ無音を挟まずループする。
- 音源は`Assets/CoyoteBattle/Resources/Audio/CoyoteBattleTitleTheme.wav`と`CoyoteBattleTheme.wav`へ配置する。
- 長時間再生時のメモリ消費を抑えるため、UnityではStreaming、Vorbis品質0.7、バックグラウンド読込として取り込む。

## 制作方法と権利

2曲は、本リポジトリの`scripts/generate_original_bgm.py`が波形、和声、旋律、音色、必要な打楽器を決定的に合成したオリジナル素材である。第三者の楽曲、旋律、編曲、録音、サンプル、生成AIサービスは使用しない。

作者、生成方法、利用条件は`Assets/CoyoteBattle/ThirdPartyNotices/OriginalBgm.txt`へ記録する。生成スクリプトと同梱WAVはCoyote Battleプロジェクトの一部として管理し、アプリへ同梱・配布できる。

## 再生制御

- `BgmPlayer`をアプリ全体で1つだけ常駐させ、AudioSourceを増やさない。
- Titleとルール説明ではTitle曲、Battle、RoundResult、GameOverではプレイ曲を再生する。
- Titleからゲーム開始、またはゲーム中からタイトル復帰した場合だけ対応曲の先頭へ切り替える。同じ曲を使う画面間、次ラウンド、再戦では先頭へ戻さず継続する。
- AudioSourceはループ再生し、音量は端末のメディア音量へ従う。OS全体の音量は変更しない。
- 表示用CameraへAudioListenerを1つだけ設定する。既存AudioListenerがある場合は追加しない。
- 選択したAudioClipが欠落した場合は警告を記録し、無音のまま画面生成とゲーム進行を継続する。
- アプリ非アクティブ化では一時停止し、復帰時はBGM設定がONの場合だけ同じ再生位置から再開する。
- シーンが再読込されても既存プレイヤーを再利用し、多重再生を防ぐ。

## BGM設定

- すべての画面の左上に共通のBGM設定ボタンを表示し、現在状態を`BGM ON`または`BGM OFF`と明記する。
- ONは緑系、OFFは橙系の背景と枠線を使い、OFFでも操作不能に見えない外観を維持する。
- 初期値はONとする。
- OFFは現在位置で即時に一時停止し、ONは同じ位置から再開する。
- 設定は`CoyoteBattle.Audio.BgmEnabled`をキーとしてPlayerPrefsへ保存し、次回起動時に復元する。
- 未保存または不正な保存値は既定値ONとして扱う。

## テスト

- EditModeで未保存時の既定値、ON/OFF保存、不正値の復旧、音源の存在、90〜120秒の長さ、Streaming設定を検証する。
- PlayModeで単一AudioListener、単一プレイヤーと単一AudioSource、Title／プレイ曲切替、同一曲の再指定、ループ設定、ON/OFFの連続操作、設定保存、中断復帰、音源欠落時の画面継続を検証する。
- Android実機でループ境界、端末ミュート、メディア音量変更、ホーム移動による中断復帰、再戦、タイトル復帰を試聴確認する。
