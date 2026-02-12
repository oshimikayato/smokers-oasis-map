# プロジェクト操作マニュアル & UIガイド

このプロジェクト「Flashback AR App」の構成、Unityエディタでの操作方法、およびアプリのカスタマイズ方法について解説します。

## 1. プロジェクト構成 (Project Structure)

主なファイルは `Assets/Scripts` フォルダに集約されています。

- **Assets/Scripts/**
  - `FlashbackManager.cs`: フラッシュバック機能の頭脳。記録と再生を管理します。
  - `SearchUIManager.cs`: 検索ボタンの動作を管理します。
  - `MockDetector.cs`: **(テスト用)** 物体検知をシミュレーションするスクリプトです。
  - `FlashbackData.cs`: データの保存形式を定義しています。
  - **Editor/**
    - `SceneSetupTool.cs`: メニューバーの [Tools] 機能を作るためのスクリプトです。

## 2. Unityエディタでの操作ガイド

自動セットアップ後のシーン (`FlashbackApp`) の主要な要素を解説します。
**Hierarchy（ヒエラルキー）ウィンドウ** で以下のオブジェクトを確認・編集できます。

### A. Searchボタンの見た目を変える
視界に表示されるボタンをカスタマイズしたい場合：

1. Hierarchyで `NRCameraRig` > `TrackingSpace` > `CenterCamera` > `HeadLockedCanvas` > **`SearchButton`** を探します。
2. **位置調整**: `Rect Transform` の Pos X, Pos Y, Pos Z を変更します。
   - 現在は `CenterCamera` の子にあるため、頭の動きに追従します。
3. **色変更**: `Image` コンポーネントの **Color** をクリックして変更します。
4. **テキスト変更**: `SearchButton` の子にある `Text` オブジェクトを選択し、`Text` コンポーネントの文字を書き換えます（例: "思い出検索"）。

### B. フラッシュバックの設定を変える
再生速度やテスト検知の間隔を調整したい場合：

1. Hierarchyで **`AppManager`** を選択します。
2. **Inspector（インスペクター）ウィンドウ** を見ます。
3. **Flashback Manager (Script)**
   - `Flashback Duration`: 画像1枚あたりの表示時間（秒）を変更できます。
4. **Mock Detector (Script)**
   - `Detection Interval`: 何秒ごとに「検知」をシミュレーションするか設定できます。
   - `Is Active`: チェックを外すと、自動検知（シミュレーション）が止まります。

## 3. アプリ操作ガイド (実機)

XREAL Beam Pro での操作フローです。

1. **起動**: アプリを起動すると、現実の風景（パススルー）が見えます。
2. **待機**: `MockDetector` が動いているため、バックグラウンドで勝手に「何かを見つけた」として記録が溜まっていきます（ログには出ますが画面には出ません）。
3. **検索**:
   - 視界の中央にある **[SEARCH]** ボタンを見ます。
   - コントローラー（スマホ）を向け、ポインター（レーザー）をボタンに合わせます。
   - コントローラーの **決定ボタン（トリガー/タップ）** を押します。
4. **体験**:
   - 過去に記録された画像が、目の前にスライドショーのように次々と表示されます。
   - 全て表示し終わると、自動的に消えます。

## 4. 今後の開発ステップ

現在は「シミュレーション（Mock）」で動いています。本番化するには以下のステップが必要です。

1. **画像認識の実装**:
   - `MockDetector.cs` を削除または無効化します。
   - 代わりに NRSDKの `NRTrackingImageDatabase` を使い、特定のマーカーや物体を認識した瞬間に `FlashbackManager.Instance.RegisterDetection(...)` を呼び出すスクリプトを作成します。

2. **UIのブラッシュアップ**:
   - UnityのUIシステム (uGUI) を使い、よりカッコいいデザインの画像やアニメーションを `SearchButton` に適用します。

---
このマニュアルは `Assets/ProjectManual.md` に保存されています。
