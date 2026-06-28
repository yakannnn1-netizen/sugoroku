using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Game.Core;
using System.Drawing;
using System.Drawing.Text;
using System.Drawing.Drawing2D;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace MonoGameUI;

public class Game1 : Microsoft.Xna.Framework.Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    // 白1ピクセルの動的テクスチャ（図形描画用）
    private Texture2D _pixel;

    // プレイヤー用ダミー画像テクスチャ
    private Texture2D _playerTexture;

    // 召喚獣用ダミー画像テクスチャ
    private Texture2D _summonTexture;

    // 特殊マス用ダミー画像テクスチャ
    private Texture2D _startTexture;
    private Texture2D _shopTexture;
    private Texture2D _cpTexture;

    // Coreのオブジェクト
    private GameManager _gameManager;
    private Dice _dice;

    // マスの描画座標などを保持
    private Dictionary<Square, Rectangle> _squareRects = new Dictionary<Square, Rectangle>();

    // プレイヤーの描画座標（スムーズ移動用）
    private Dictionary<Player, Vector2> _playerPositions = new Dictionary<Player, Vector2>();

    // UI入力待機用
    private TaskCompletionSource<int> _uiSelectionTcs;
    private List<UIButton> _currentButtons = new List<UIButton>();

    // 画面下部に流れるメッセージログ用
    private List<string> _messageLog = new List<string>();
    private const int MaxLogLines = 5;

    // マウスの前の状態（クリックエッジ検出用）
    private MouseState _prevMouseState;

    // 非同期ループが動いているか
    private bool _isGameRunning = false;

    // 描画関連の定数
    private const int SquareSize = 64;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = 800;
        _graphics.PreferredBackBufferHeight = 600;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        base.Initialize();

        var mapFactory = new MapFactory();
        var board = mapFactory.CreateDefaultMap();
        _gameManager = new GameManager(board);
        _dice = new Dice();

        // 盤面の描画レイアウトを計算（円形配置）
        int totalSquares = board.AllSquares.Count;
        float radius = 200f;
        Vector2 center = new Vector2(400, 300);

        for (int i = 0; i < totalSquares; i++)
        {
            var square = board.AllSquares[i];
            float angle = i * MathHelper.TwoPi / totalSquares;

            int x = (int)(center.X + MathF.Cos(angle) * radius) - SquareSize / 2;
            int y = (int)(center.Y + MathF.Sin(angle) * radius) - SquareSize / 2;

            _squareRects[square] = new Rectangle(x, y, SquareSize, SquareSize);
        }

        // Coreのイベント紐付け
        _gameManager.OnRouteSelectionRequested = HandleRouteSelectionAsync;
        _gameManager.OnPlayerMovingAsync = HandlePlayerMovingAsync;

        foreach (var square in board.AllSquares)
        {
            if (square is PropertySquare propertySquare)
            {
                propertySquare.OnPurchaseRequested = HandlePurchaseRequestAsync;
                propertySquare.OnTollPaid = HandleTollPaidAsync;
                propertySquare.OnUpgradeRequested = HandleUpgradeRequestAsync;
            }
            else if (square is StartSquare startSquare)
            {
                startSquare.OnBonusAwarded = HandleBonusAwardedAsync;
                startSquare.OnShopEntered = HandleShopEnteredAsync;
            }
            else if (square is ShopSquare shopSquare)
            {
                shopSquare.OnShopEntered = HandleShopEnteredAsync;
            }
        }

        var p1 = new Player("プレイヤー1", 1000);
        var p2 = new Player("プレイヤー2", 1000);
        _gameManager.AddPlayer(p1);
        _gameManager.AddPlayer(p2);

        // 初期座標のセット
        _playerPositions[p1] = GetSquareCenter(board.StartSquare);
        _playerPositions[p2] = GetSquareCenter(board.StartSquare);

        // ゲームループ起動
        _isGameRunning = true;
        _ = GameLoopAsync();
    }

    private Vector2 GetSquareCenter(Square square)
    {
        if (_squareRects.TryGetValue(square, out var rect))
        {
            return new Vector2(rect.Center.X, rect.Center.Y);
        }
        return Vector2.Zero;
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // 1x1の白テクスチャを生成
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        // プレイヤーと召喚獣画像のロード
        try
        {
            using (var stream = System.IO.File.OpenRead("player.png"))
                _playerTexture = Texture2D.FromStream(GraphicsDevice, stream);
        }
        catch
        {
            _playerTexture = _pixel;
        }

        try
        {
            using (var stream = System.IO.File.OpenRead("summon.png"))
                _summonTexture = Texture2D.FromStream(GraphicsDevice, stream);
        }
        catch
        {
            _summonTexture = _pixel;
        }

        try
        {
            using (var stream = System.IO.File.OpenRead("start.png")) _startTexture = Texture2D.FromStream(GraphicsDevice, stream);
        }
        catch { _startTexture = _pixel; }

        try
        {
            using (var stream = System.IO.File.OpenRead("shop.png")) _shopTexture = Texture2D.FromStream(GraphicsDevice, stream);
        }
        catch { _shopTexture = _pixel; }

        try
        {
            using (var stream = System.IO.File.OpenRead("cp.png")) _cpTexture = Texture2D.FromStream(GraphicsDevice, stream);
        }
        catch { _cpTexture = _pixel; }
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        MouseState currentMouse = Mouse.GetState();

        // 左クリックが押された瞬間を判定
        if (currentMouse.LeftButton == ButtonState.Pressed && _prevMouseState.LeftButton == ButtonState.Released)
        {
            // 現在表示されているボタンがクリックされたか調べる
            foreach (var btn in _currentButtons)
            {
                if (btn.Bounds.Contains(currentMouse.Position))
                {
                    // Tcsが未完了なら結果をセットして入力を終わらせる
                    if (_uiSelectionTcs != null && !_uiSelectionTcs.Task.IsCompleted)
                    {
                        // 押されたボタンのインデックスを返す
                        _uiSelectionTcs.SetResult(btn.Index);
                        break;
                    }
                }
            }
        }

        _prevMouseState = currentMouse;
        base.Update(gameTime);
    }

    // --- 文字テクスチャのキャッシュ ---
    private Dictionary<string, Texture2D> _textCache = new Dictionary<string, Texture2D>();

    private Texture2D GetTextTexture(string text, int width, int height)
    {
        string key = $"{text}_{width}_{height}";
        if (_textCache.TryGetValue(key, out Texture2D tex)) return tex;

        tex = CreateTextTexture(text, width, height);
        _textCache[key] = tex;
        return tex;
    }

    // System.Drawing.Common を使用して文字列から Texture2D を生成 (Premultiplied Alpha対応)
    private Texture2D CreateTextTexture(string text, int width, int height)
    {
        using (var bitmap = new Bitmap(width, height))
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(System.Drawing.Color.Transparent);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = TextRenderingHint.AntiAlias;

            using (var font = new Font("Arial", 12, FontStyle.Bold))
            using (var brush = new SolidBrush(System.Drawing.Color.White))
            {
                var format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                graphics.DrawString(text, font, brush, new System.Drawing.RectangleF(0, 0, width, height), format);
            }

            var texture = new Texture2D(GraphicsDevice, width, height);
            var data = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    // MonoGame (XNA) のデフォルトブレンドステートは Premultiplied Alpha を期待するため、RGBにAを掛ける
                    float alpha = pixel.A / 255f;
                    data[y * width + x] = new Color((int)(pixel.R * alpha), (int)(pixel.G * alpha), (int)(pixel.B * alpha), pixel.A);
                }
            }
            texture.SetData(data);
            return texture;
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin();

        // 盤面（マス）の描画
        foreach (var kvp in _squareRects)
        {
            var square = kvp.Key;
            var rect = kvp.Value;

            if (square is StartSquare)
            {
                _spriteBatch.Draw(_startTexture, rect, Color.White);
            }
            else if (square is CheckpointSquare)
            {
                _spriteBatch.Draw(_cpTexture, rect, Color.White);
            }
            else if (square is ShopSquare)
            {
                _spriteBatch.Draw(_shopTexture, rect, Color.White);
            }
            else
            {
                // PropertySquareなど通常の土地マス
                Color sqColor = Color.LightGray;
                if (square is PropertySquare p && p.Owner != null) sqColor = GetPlayerColor(p.Owner); // 誰かの土地

                // 枠線を白くし、中をマスの色（またはグレー）にする反転
                _spriteBatch.Draw(_pixel, rect, Color.White); // 外枠は白
                var innerRect = new Rectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4);
                _spriteBatch.Draw(_pixel, innerRect, sqColor); // 中身は色付き
            }

            // プロパティマスなら価格や徴収額を表示
            if (square is PropertySquare prop)
            {
                string infoText = prop.Owner == null ? $"{prop.BaseValue}C" : $"Toll:{prop.CalculateToll()}";
                Texture2D infoTex = GetTextTexture(infoText, rect.Width + 20, 20);
                var infoRect = new Rectangle(rect.X - 10, rect.Y + rect.Height + 5, rect.Width + 20, 20);
                _spriteBatch.Draw(infoTex, infoRect, Color.White);

                // 配置されている召喚獣の描画
                if (prop.PlacedSummon != null)
                {
                    // 召喚獣のダミー画像
                    var summonImgRect = new Rectangle(rect.X + rect.Width / 2 - 10, rect.Y - 25, 20, 20);
                    _spriteBatch.Draw(_summonTexture, summonImgRect, Color.White);

                    // 召喚獣の名前
                    Texture2D summonTex = GetTextTexture(prop.PlacedSummon.Name, rect.Width + 40, 20);
                    var summonRect = new Rectangle(rect.X - 20, rect.Y - 45, rect.Width + 40, 20);
                    _spriteBatch.Draw(summonTex, summonRect, Color.White);
                }
            }
        }

        // プレイヤーの描画
        if (_isGameRunning)
        {
            // プレイヤー1と2で位置を少しずらして重ならないようにする
            var players = _gameManager.Players;
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (_playerPositions.TryGetValue(player, out Vector2 pos))
                {
                    int offsetX = (i == 0) ? -15 : 15;
                    int offsetY = (i == 0) ? -15 : 15;
                    var pRect = new Rectangle((int)pos.X + offsetX - 10, (int)pos.Y + offsetY - 10, 20, 20);
                    _spriteBatch.Draw(_playerTexture, pRect, GetPlayerColor(player));
                }
            }
        }

        // UI（ボタン）の描画
        foreach (var btn in _currentButtons)
        {
            _spriteBatch.Draw(_pixel, btn.Bounds, Color.White); // 枠を白
            var innerBtnRect = new Rectangle(btn.Bounds.X + 2, btn.Bounds.Y + 2, btn.Bounds.Width - 4, btn.Bounds.Height - 4);
            _spriteBatch.Draw(_pixel, innerBtnRect, btn.Color); // 中を黒(または指定色)

            if (btn.TextTexture != null)
            {
                _spriteBatch.Draw(btn.TextTexture, btn.Bounds, Color.White);
            }
        }

        _spriteBatch.End();

        // 画面下部にログを描画する
        DrawMessageLog();

        // 画面上部にプレイヤー情報を描画する
        DrawPlayerHUD();

        // タイトルバーには基本的な情報を表示
        Window.Title = "Sugoroku Game (Chocobo Land Style)";

        base.Draw(gameTime);
    }

    private void DrawPlayerHUD()
    {
        var player = _gameManager.GetCurrentPlayer();
        if (player == null) return;

        _spriteBatch.Begin();

        // HUDの背景パネルを描画
        int panelHeight = 40;
        int panelWidth = _graphics.PreferredBackBufferWidth;
        var hudRect = new Rectangle(0, 0, panelWidth, panelHeight);
        _spriteBatch.Draw(_pixel, hudRect, new Color(0, 0, 0, 200));

        // プレイヤーの情報を文字列として構築
        string infoText = $"{player.Name} | クリスタル: {player.Crystal}C | チェックポイント: {player.GetVisitedCheckpointCount()}/4";

        // 状態異常があれば追記
        if (player.SealTurns > 0) infoText += $" | 【封印】あと{player.SealTurns}T";
        else if (player.SleepTurns > 0) infoText += " | 【睡眠】";

        // テキストを描画
        Texture2D textTex = GetTextTexture(infoText, panelWidth - 20, 30);
        var rect = new Rectangle(10, 5, panelWidth - 20, 30);
        _spriteBatch.Draw(textTex, rect, Color.White);

        _spriteBatch.End();
    }

    private void DrawMessageLog()
    {
        _spriteBatch.Begin();

        // ログの背景パネルを描画
        int panelHeight = 120;
        int panelWidth = _graphics.PreferredBackBufferWidth;
        int panelY = _graphics.PreferredBackBufferHeight - panelHeight;
        var logRect = new Rectangle(0, panelY, panelWidth, panelHeight);

        // 半透明の黒背景
        _spriteBatch.Draw(_pixel, logRect, new Color(0, 0, 0, 150));

        // テキストを描画
        int lineHeight = 20;
        for (int i = 0; i < _messageLog.Count; i++)
        {
            Texture2D textTex = GetTextTexture(_messageLog[i], panelWidth - 20, lineHeight);
            var rect = new Rectangle(10, panelY + 10 + i * lineHeight, panelWidth - 20, lineHeight);
            _spriteBatch.Draw(textTex, rect, Color.White);
        }

        _spriteBatch.End();
    }

    private Color GetPlayerColor(Player player)
    {
        // プレイヤー１は緑、２は赤３は青４は黄色
        if (player.Name == "プレイヤー1") return Color.Green;
        if (player.Name == "プレイヤー2") return Color.Red;
        if (player.Name == "プレイヤー3") return Color.Blue;
        if (player.Name == "プレイヤー4") return Color.Yellow;
        return Color.White;
    }

    // ==========================================
    // 非同期ゲームループとCoreとの連携
    // ==========================================

    private void SetMessage(string message)
    {
        _messageLog.Add(message);
        if (_messageLog.Count > MaxLogLines)
        {
            _messageLog.RemoveAt(0); // 古いものを削除
        }
        Console.WriteLine($"[Game] {message}");
    }

    private async Task GameLoopAsync()
    {
        bool isGameOver = false;
        while (!isGameOver)
        {
            var player = _gameManager.GetCurrentPlayer();
            player.TurnCount++; // ターン数をカウントアップ

            SetMessage($"=== {player.Name} のターン (所持金:{player.Crystal}C, {player.TurnCount}ターン目) ===");
            await Task.Delay(1000); // ターン開始時のウェイト

            if (player.SleepTurns > 0)
            {
                SetMessage($"{player.Name} は眠っている...（1回休み）");
                player.DecrementStatusEffects();
                await Task.Delay(1500);
                _gameManager.NextTurn();
                continue;
            }

            bool turnActive = true;
            bool actionTaken = false; // ループ内で移動アクションが行われたかを管理

            while (turnActive)
            {
                SetMessage($"{player.Name} のターン: 行動を選択");

                var actionOptions = new List<string>();
                actionOptions.Add("サイコロを振る");

                // 魔法は2周目以降(TurnCount >= 2)にインベントリにあれば使える
                bool canUseMagic = player.TurnCount >= 2 && player.Inventory.Magics.Count > 0;
                if (canUseMagic)
                {
                    actionOptions.Add("魔法を使う");
                }

                // 「ゲームスタート時 (TurnCount == 1 最初の行動前)」のみショップを利用可能
                // ※2周目以降(TurnCount >= 2)はショップ・スタートマスから動くターンには購入不可
                bool isGameStart = player.TurnCount == 1 && !actionTaken;
                if (isGameStart)
                {
                    actionOptions.Add("ショップを利用");
                }

                int selectedIndex = await ShowSelectionUIAsync(actionOptions);
                string selectedAction = actionOptions[selectedIndex];

                if (selectedAction == "サイコロを振る")
                {
                    actionTaken = true;
                    int roll = _dice.Roll();
                    SetMessage($"サイコロの目: 【 {roll} 】");
                    await Task.Delay(1000);

                    // Manager側で1マス進むごとに HandlePlayerMovingAsync が呼ばれ、アニメーション待機が行われる
                    await _gameManager.MovePlayerAsync(player, roll);

                    if (player.CurrentSquare is StartSquare && player.GetVisitedCheckpointCount() == 0 && player.Crystal >= 3000)
                    {
                        // 仮の勝利条件: クリスタル3000以上でスタートに戻る
                        SetMessage($"祝！ {player.Name} が勝利しました！");
                        isGameOver = true;
                    }

                    turnActive = false;
                }
                else if (selectedAction == "ショップを利用")
                {
                    if (player.CurrentSquare is ShopSquare shop)
                    {
                        await shop.OnShopEntered(player, shop);
                    }
                }
                else if (selectedAction == "魔法を使う")
                {
                    await HandleUseMagicAsync(player);
                }
            }

            if (!isGameOver) _gameManager.NextTurn();
        }
    }

    // --- 汎用的な選択肢待機メソッド ---
    private async Task<int> ShowSelectionUIAsync(List<string> options)
    {
        _currentButtons.Clear();
        int btnWidth = 250; // テキストが収まるように広げる
        int btnHeight = 40;
        int startX = 10;
        int startY = 10;

        for (int i = 0; i < options.Count; i++)
        {
            var rect = new Rectangle(startX, startY + i * (btnHeight + 10), btnWidth, btnHeight);
            Texture2D textTex = GetTextTexture(options[i], btnWidth, btnHeight);

            _currentButtons.Add(new UIButton {
                Index = i,
                Bounds = rect,
                Color = Color.Black,
                Text = options[i],
                TextTexture = textTex
            });
            // Console.WriteLineに出力して、どのボタンが何のアクションかを知らせる
            Console.WriteLine($"Button {i}: {options[i]}");
        }

        _uiSelectionTcs = new TaskCompletionSource<int>();
        int selectedIndex = await _uiSelectionTcs.Task;

        _currentButtons.Clear();
        return selectedIndex;
    }

    // --- 1マス移動のアニメーション ---
    private async Task HandlePlayerMovingAsync(Player player, Square targetSquare)
    {
        Vector2 startPos = _playerPositions[player];
        Vector2 endPos = GetSquareCenter(targetSquare);

        int frames = 15; // 1マス移動にかけるフレーム数(目安)
        int delayPerFrame = 16; // 約60fps

        for (int i = 1; i <= frames; i++)
        {
            float t = (float)i / frames;

            // 跳ねるような動き (サイン波)
            float hopY = (float)Math.Sin(t * Math.PI) * -30f;

            Vector2 currentPos = Vector2.Lerp(startPos, endPos, t);
            currentPos.Y += hopY;

            _playerPositions[player] = currentPos;

            await Task.Delay(delayPerFrame);
        }

        _playerPositions[player] = endPos;
    }

    // ==========================================
    // カスタムUIイベントハンドラ
    // ==========================================

    private async Task HandleUseMagicAsync(Player player)
    {
        // 1. 魔法を選択
        var magicOptions = player.Inventory.Magics.Select(m => m.Name).ToList();
        magicOptions.Insert(0, "やめる");
        SetMessage("どの魔法を使いますか？");
        int magicChoice = await ShowSelectionUIAsync(magicOptions);

        if (magicChoice > 0)
        {
            var magicToCast = player.Inventory.Magics[magicChoice - 1];

            // 2. ターゲットを選択
            var targetOptions = _gameManager.Players.Select(p => p.Name).ToList();
            targetOptions.Insert(0, "やめる");
            SetMessage("誰に対して使いますか？");
            int targetChoice = await ShowSelectionUIAsync(targetOptions);

            if (targetChoice > 0)
            {
                var targetPlayer = _gameManager.Players[targetChoice - 1];

                // 3. 魔法の発動
                var result = magicToCast.Cast(player, targetPlayer);
                SetMessage(result.Message);
                await Task.Delay(2000);

                // 消費
                // 使用した魔法は、相殺・無効化に関わらずインベントリから減らす
                player.Inventory.RemoveMagic(magicToCast);
            }
        }
    }

    // --- Core のイベントハンドラ ---

    private async Task<Square> HandleRouteSelectionAsync(Player player, List<Square> availableSquares)
    {
        SetMessage("進むルートを選択してください (Consoleログ参照)");
        var options = availableSquares.Select(s => s.Name).ToList();
        int idx = await ShowSelectionUIAsync(options);
        return availableSquares[idx];
    }

    private async Task HandlePurchaseRequestAsync(Player player, PropertySquare prop)
    {
        if (player.Inventory.Summons.Count == 0)
        {
            SetMessage($"空き地『{prop.Name}』に到着したが、配置できる召喚獣を持っていない...");
            await Task.Delay(1500);
            return;
        }

        SetMessage($"『{prop.Name}』({prop.BaseValue} C) を獲得しますか？(Consoleログ参照)");
        var options = player.Inventory.Summons.Select(s => $"{s.Name} (倍率: {s.TollMultiplier})").ToList();
        options.Insert(0, "やめる");

        int choiceIndex = await ShowSelectionUIAsync(options);

        if (choiceIndex > 0)
        {
            var targetSummon = player.Inventory.Summons[choiceIndex - 1];
            if (prop.TryBuy(player, targetSummon))
            {
                player.Inventory.RemoveSummon(targetSummon);
                SetMessage($"{prop.Name} を購入し、{targetSummon.Name} を配置しました！");
            }
            else
            {
                SetMessage("クリスタルが足りません！");
            }
            await Task.Delay(1500);
        }
    }

    private async Task HandleUpgradeRequestAsync(Player player, PropertySquare prop)
    {
        int upgradeCost = prop.BaseValue * prop.Level;
        SetMessage($"自領地『{prop.Name}』(Lv {prop.Level}) {upgradeCost}C で増資しますか？");
        var options = new List<string> { "増資する", "やめる" };

        int choice = await ShowSelectionUIAsync(options);

        if (choice == 0)
        {
            if (prop.TryUpgrade(player))
            {
                SetMessage($"{prop.Name} が Lv{prop.Level} になりました！");
            }
            else
            {
                SetMessage("クリスタルが足りません...");
            }
            await Task.Delay(1500);
        }
    }

    private async Task HandleTollPaidAsync(Player player, PropertySquare prop, int amount)
    {
        string msg = $"{prop.Owner.Name} の領地『{prop.Name}』だ！ {amount} C 支払った...";
        if (prop.PlacedSummon != null)
        {
            msg = $"召喚獣 {prop.PlacedSummon.Name} が立ちはだかる！ " + msg;
        }
        SetMessage(msg);
        await Task.Delay(2500);
    }

    private async Task HandleShopEnteredAsync(Player player, ShopSquare shop)
    {
        bool inShop = true;
        while (inShop)
        {
            SetMessage("【ショップ】 何を買いますか？ (Consoleログ参照)");
            int choice = await ShowSelectionUIAsync(new List<string> { "出る", "召喚獣", "魔法" });

            if (choice == 0)
            {
                inShop = false;
            }
            else if (choice == 1) // 召喚獣
            {
                bool buyingSummons = true;
                while (buyingSummons)
                {
                    var options = SummonCatalog.AllSummons.Select(s => $"{s.Name} ({s.Cost}C)").ToList();
                    options.Insert(0, "戻る");
                    SetMessage($"どの召喚獣を買いますか？ (所持金:{player.Crystal}C)");
                    int buyChoice = await ShowSelectionUIAsync(options);

                    if (buyChoice == 0)
                    {
                        buyingSummons = false; // 戻る
                    }
                    else
                    {
                        var target = SummonCatalog.AllSummons[buyChoice - 1];
                        if (shop.TryBuySummon(player, target))
                        {
                            SetMessage($"{target.Name} を購入しました！");
                        }
                        else
                        {
                            SetMessage("クリスタル不足か、枠がいっぱいです。");
                        }
                        await Task.Delay(1500);
                    }
                }
            }
            else if (choice == 2) // 魔法
            {
                bool buyingMagics = true;
                while (buyingMagics)
                {
                    var options = MagicCatalog.AllMagics.Select(m => $"{m.Name} ({m.Cost}C)").ToList();
                    options.Insert(0, "戻る");
                    SetMessage($"どの魔法を買いますか？ (所持金:{player.Crystal}C)");
                    int buyChoice = await ShowSelectionUIAsync(options);

                    if (buyChoice == 0)
                    {
                        buyingMagics = false; // 戻る
                    }
                    else
                    {
                        var target = MagicCatalog.AllMagics[buyChoice - 1];
                        if (shop.TryBuyMagic(player, target))
                        {
                            SetMessage($"{target.Name} を購入しました！");
                        }
                        else
                        {
                            SetMessage("クリスタル不足か、枠がいっぱいです。");
                        }
                        await Task.Delay(1500);
                    }
                }
            }
        }
    }

    private async Task HandleBonusAwardedAsync(Player player, int amount, int count)
    {
        string msg = $"チェックポイントを {count} 個集めて帰還！報酬 {amount} C を獲得！";
        if (count == 4) msg = "★パーフェクトボーナス！★ " + msg;

        SetMessage(msg);
        await Task.Delay(2500);
    }
}

// 簡易ボタン構造体
public struct UIButton
{
    public int Index;
    public Rectangle Bounds;
    public Color Color;
    public string Text;
    public Texture2D TextTexture;
}
