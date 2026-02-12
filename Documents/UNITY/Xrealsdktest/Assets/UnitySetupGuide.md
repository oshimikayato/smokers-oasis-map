# Unity ARアプリ (Flashback機能) セットアップガイド

このドキュメントでは、XREAL Beam Pro向けのARアプリ（Flashback機能付き）をUnityでセットアップし、ビルドするまでの手順を解説します。
Unity初心者の方でも進められるよう、自動化ツールを用意しています。

## 1. 事前準備

以下の環境が整っていることを確認してください。
- **Unity Editor**: 推奨バージョン (例: 2022.3.x LTS)
- **Android Build Support**: Unity Hubでモジュールとしてインストール済みであること
  - Android SDK & NDK Tools
  - OpenJDK
- **NRSDK**: プロジェクトにインポート済みであること（既にAssetsフォルダにあります）

## 2. シーンの自動セットアップ

手動でカメラやボタンを配置するのは複雑なため、**自動セットアップツール**を作成しました。以下の手順で実行してください。

1. Unity Editorを開きます。
2. 上部メニューバーにある **[Tools]** をクリックします。
3. **[Setup Flashback App]** を選択します。

> **何が起きますか？**
> - 新しいシーン `FlashbackApp` が作成されます。
> - **NRCameraRig**: ARカメラ（XREALグラスの視界）が配置されます。
> - **NRInput**: コントローラー入力システムが配置されます。
> - **HeadLockedCanvas**: 視界に追従するUI（Searchボタン）が配置されます。
> - **AppManager**: アプリの機能を管理するスクリプト（FlashbackManagerなど）が配置されます。

## 3. ビルド設定 (Build Settings)

アプリをAndroid（XREAL Beam Pro）用に設定します。

1. メニューの **[File]** > **[Build Settings]** を開きます。
2. **Platform** のリストから **Android** を選択します。
3. 右下の **[Switch Platform]** ボタンを押します（数分かかる場合があります）。
4. **Scenes In Build** のリストに、先ほど作成した `Assets/Scenes/FlashbackApp.unity` がチェック付きで追加されていることを確認します。
   - もし無ければ、[Add Open Scenes] ボタンを押して追加してください。

## 4. プレイヤー設定 (Player Settings)

NRSDKを正しく動作させるための重要な設定です。
**[Build Settings]** ウィンドウの左下にある **[Player Settings...]** をクリックします。

### A. Other Settings (その他の設定)
左側のタブで **Player** を選択し、**Other Settings** を展開します。

- **Rendering**
  - **Auto Graphics API**: チェックを外す
  - **Graphics APIs**: **OpenGLES3** のみをリストに残す（Vulkanが含まれている場合は選択して「-」ボタンで削除してください。NRSDKはVulkanと相性が悪い場合があります）。
  - **Color Space**: **Linear** に設定

- **Configuration**
  - **Scripting Backend**: **IL2CPP** に変更
  - **Target Architectures**: **ARM64** にチェックを入れる（ARMv7は外す）
  - **Active Input Handling**: **Input System Package (New)** または **Both** に設定

- **Identification**
  - **Minimum API Level**: **Android 8.0 'Oreo' (API level 26)** 以上に設定
  - **Target API Level**: **Automatic (Highest installed)**

### B. XR Plug-in Management
左側のタブで **XR Plug-in Management** を選択します。
- **Android** タブ（ドロイド君アイコン）を選択
- **NRSDK** にチェックが入っていることを確認

## 5. ビルドと実行

1. XREAL Beam Pro（またはAndroid端末）をUSBケーブルでPCに接続します。
   - 端末側で「ファイル転送」モードなどを許可し、USBデバッグがONになっていることを確認してください。
2. **Build Settings** ウィンドウに戻ります。
3. **Run Device** のリストに接続した端末が表示されているか確認し、選択します（表示されない場合は [Refresh] を押してください）。
4. **[Build And Run]** をクリックします。
5. ファイル保存ダイアログが出るので、適当な名前（例: `FlashbackApp.apk`）を付けて保存します。

ビルドが完了すると、自動的に端末でアプリが起動します。

## 6. アプリの使い方

1. アプリが起動すると、AR空間（カメラ映像）が表示されます。
2. 視界の中央付近に **[SEARCH]** ボタンが常に表示されます（頭を動かすとついてきます）。
3. XREAL Beam Proのコントローラー（またはスマホ画面のバーチャルコントローラー）を使い、ポインターをボタンに合わせてクリック（トリガー）します。
4. **フラッシュバック機能**:
   - 過去24時間に「検出」された物体の画像が、次々と目の前に表示されます。
   - *現在はテスト用として、ランダムな色の画像が表示される仕様になっています。*

---
ご不明な点があれば、いつでもAIアシスタントにお尋ねください。
