# ARマーカー（画像認識）セットアップガイド

本物の画像（ポスターやカードなど）を認識してフラッシュバックに記録するための手順です。

## 1. 画像データベースの作成

Unityエディタで、認識させたい画像を登録します。

1.  **Project** ウィンドウで、適当なフォルダ（例: `Assets/Textures`）を作成し、認識させたい画像ファイル（.jpg, .png）を入れます。
2.  **Project** ウィンドウで右クリックし、**Create > NRSDK > TrackingImageDatabase** を選択します。
    - 名前は `MyImageDatabase` などにします。
3.  作成した `MyImageDatabase` を選択し、Inspectorの **Tracking Image Database Config** を見ます。
4.  **List is Empty** の右下の「+」ボタンを押して、エントリを追加します。
5.  **Name** に名前を入力し、**Texture** に手順1の画像をドラッグ＆ドロップします。
    - **Width** には、実物の横幅（メートル単位）を入力します（例: 10cmなら `0.1`）。
    - これを認識させたい画像の数だけ繰り返します。

## 2. セッション設定への登録

NRSDKにこのデータベースを使うよう伝えます。

1.  Hierarchyで **`NRCameraRig`** を選択します。
2.  Inspectorの **NRSessionBehaviour** コンポーネントを探します。
3.  **Session Config** プロパティにある設定ファイル（例: `NRSessionConfig`）をダブルクリックして開きます。
    - もし無ければ、Projectウィンドウで右クリック > **Create > NRSDK > SessionConfig** で新規作成し、割り当ててください。
4.  開いたConfigの **Image Tracking Mode** を **Enable** にします。
5.  **Tracking Image Database** の欄に、手順1で作った `MyImageDatabase` をドラッグ＆ドロップします。

## 3. アプリへの登録（Flashback用）

フラッシュバック機能にも同じ画像を登録します。

1.  Hierarchyで **`AppManager`** を選択します。
2.  Inspectorの **Real Image Detector (Script)** を見ます。
3.  **Tracking Database**: 手順1で作った `MyImageDatabase` をドラッグ＆ドロップします。
4.  **Reference Images**:
    - リストを開き、認識させたい画像（Texture）を全て登録します。
    - **重要**: 画像のファイル名（name）は、データベースの **Name** と一致させてください。

## 4. 実行確認

1.  **Mock Detector** の `Is Active` がチェック外れていることを確認します（自動セットアップで外れています）。
2.  ビルドして実機で実行します。
3.  登録した画像をカメラで見ます。
4.  「SEARCH」ボタンを押すと、その画像が表示されるはずです。
