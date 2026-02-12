# Pythonサーバーの別PC移行ガイド

現在のサーバー構成をそのまま別のPCに移すための手順書です。

## 1. 移行用ファイルの準備

現在の開発用PCに、サーバー一式をまとめたZIPファイルを作成済みです。

**ZIPファイルの場所:**
`C:\Users\oshim\Documents\UNITY\Xrealsdktest\Server_Package.zip`

このファイルをUSBメモリやGoogle Drive等で**新しPC**にコピーしてください。

## 2. 新PCでのセットアップ

1. **Pythonのインストール**
   - [Python公式サイト](https://www.python.org/downloads/) から最新版（Python 3.10以上推奨）をダウンロードしてインストールしてください。
   - インストーラーの一番下にある **"Add Python to PATH"** に必ずチェックを入れてください。

2. **ファイルの展開**
   - `Server_Package.zip` を適当な場所（例: `C:\Server` など）に解凍してください。

3. **ライブラリのインストール**
   - コマンドプロンプト（またはPowerShell）を開きます。
   - 解凍したフォルダに移動します。
     ```cmd
     cd C:\Server  （例）
     ```
   - 以下のコマンドを実行して必要なライブラリをインストールします。
     ```cmd
     pip install -r requirements.txt
     ```
     ※ エラーが出る場合は `pip install flask opencv-python ultralytics numpy requests` を直接実行してみてください。

## 3. ファイアウォールの設定（重要）

外部（スマホやUnity）からアクセスできるようにポート5000を開放します。
PowerShellを**管理者として実行**し、以下のコマンドを入力してください：

```powershell
New-NetFirewallRule -Name "FlaskServer" -DisplayName "Python Flask Server (5000)" -Direction Inbound -Action Allow -Protocol TCP -LocalPort 5000
```
または、Windowsセキュリティのファイアウォール設定で `python.exe` の通信をプライベート/パブリック共に許可してください。

## 4. サーバーの起動

コマンドプロンプトで以下を実行します：

```cmd
python server.py
```

正常に起動すると以下のようなログが表示されます：
```
 * Running on all addresses (0.0.0.0)
 * Running on http://127.0.0.1:5000
 * Running on http://192.168.1.15:5000  <-- このIPアドレスをメモ！
```

## 5. Unity側の設定変更

最後に、Unityアプリが新PCのサーバーに接続するように設定を変更します。

1. **Unityエディタ** で `Tools` > `Scene Setup Tool v4` を開きます。
2. **Settings** タブを選択します。
3. **Home Server IP** の欄に、手順4でメモした新PCのIPアドレス（例: `http://192.168.1.15:5000`）を入力します。
4. **Set** ボタンを押して保存します。

これで移行完了です！スマホ実機でテストしてください。
