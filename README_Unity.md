# チョコボランド風すごろくゲーム Unity組み込みガイド

このプロジェクトのCoreロジック（C#）とUnity用スクリプトを使用して、Unity上でゲームを動かすためのセットアップ手順を説明します。
Unityの開発環境（Unity HubおよびUnity Editor）が手元のPCにインストールされている必要があります。

## 1. Unityプロジェクトの作成とファイルのインポート

1. **Unity Hub** を開き、「新しいプロジェクト」を作成します。テンプレートは「3D (Core)」を選択してください。
2. プロジェクトが開いたら、プロジェクトウィンドウの `Assets` フォルダ内に `Scripts` フォルダを作成します。
3. このリポジトリの以下のC#スクリプトを、作成した `Scripts` フォルダにドラッグ＆ドロップしてインポートします。
   - `Game/Core/` フォルダ内にあるすべての `.cs` ファイル（`GameManager.cs`, `Player.cs`, `Square.cs` などすべて）
   - `Game/UnityScripts/` フォルダ内にある以下の4つのファイル：
     - `BoardRenderer.cs`
     - `PlayerMover.cs`
     - `SugorokuGameController.cs`
     - `SugorokuUIManager.cs`
   - ※ `Game/UI/Program.cs` はコンソールアプリ用のファイルなので**Unityにはインポートしないでください**。

## 2. プレハブ（3Dモデル・見た目）の作成

Unityのヒエラルキー（Hierarchy）で右クリックし、以下の3Dオブジェクトを作成してプレハブ化（プロジェクトウィンドウにドラッグ＆ドロップ）します。プレハブ化したらシーン上のオブジェクトは削除して構いません。

### マス用のプレハブ（4種類）
1. `3D Object > Cube` などを作成します。
2. 名前を `StartSquarePrefab`, `PropertySquarePrefab`, `ShopSquarePrefab`, `CheckpointSquarePrefab` のように変更し、それぞれプレハブ化します。
   - 分かりやすいようにマテリアルを作成し、色を変えておくことをお勧めします（例：スタートは赤、ショップは青など）。

### プレイヤー用のプレハブ（PlayerMover）
1. `3D Object > Sphere` などを生成します（チョコボの3Dモデルがあればそれをインポートしてください）。
2. このオブジェクトに `PlayerMover` スクリプトをアタッチします。
3. これをプロジェクトウィンドウにドラッグしてプレハブ化し `PlayerMoverPrefab` と名付けます。

## 3. UI（Canvas）の作成

1. ヒエラルキーで右クリックし `UI > Canvas` を作成します。
2. Canvasの子オブジェクトとして `SugorokuUIManager` に必要なUIを配置します。

**必要なUI構造の例:**
- **MessageWindow** (Image等のパネル)
  - Text (メッセージ表示用)
- **ActionPanel** (Image等のパネル)
  - Button (サイコロを振る用)
  - Button (ショップ用)
  - Button (ターン終了用)
- **SelectionPanel** (Image等のパネル)
  - Button 1 ～ 3 (選択肢用、それぞれの子にTextを持たせる)
- **StatusHUD** (空のオブジェクト等)
  - Text (プレイヤー名用)
  - Text (クリスタル所持数用)
  - Text (状態異常表示用)

3. Canvas または新しく作成した空のGameObject（名前を `UIManager` などにする）に `SugorokuUIManager` スクリプトをアタッチし、インスペクターから先ほど作成したUIオブジェクト（ButtonやTextなど）をドラッグして割り当てます。

## 4. ゲームコントローラーのセットアップ

1. ヒエラルキーに空のGameObjectを作成し、名前を `GameController` にします。
2. `GameController` に以下のスクリプトをアタッチします。
   - `SugorokuGameController`
   - `BoardRenderer`
3. インスペクターから `GameController` の設定を行います。
   - **BoardRenderer**:
     - `StartSquarePrefab` などの各フィールドに、手順2で作ったマスのプレハブを割り当てます。
   - **SugorokuGameController**:
     - `BoardRenderer` に自分自身のBoardRendererを割り当てます。
     - `UiManager` に手順3で作ったUIManagerを割り当てます。
     - `PlayerMoverPrefab` に手順2で作ったプレイヤーのプレハブを割り当てます。

## 5. ゲームの実行

準備が完了したら、Unityエディタ上部にある **再生ボタン（Play）** を押してください。
円形にマスが自動配置され、UIのサイコロボタンを押すことでキャラクターがマスを跳ねて進むチョコボランド風のゲームが開始されます。
