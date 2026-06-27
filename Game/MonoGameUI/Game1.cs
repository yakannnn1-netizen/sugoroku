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

    // Coreのオブジェクト
    private GameManager _gameManager;
    private Dice _dice;

    // マスの描画座標などを保持
    private Dictionary<Square, Rectangle> _squareRects = new Dictionary<Square, Rectangle>();

    // UI入力待機用
    private TaskCompletionSource<int> _uiSelectionTcs;
    private List<UIButton> _currentButtons = new List<UIButton>();
    private string _currentMessage = "ゲーム起動中...";

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

        // ゲームループ起動
        _isGameRunning = true;
        _ = GameLoopAsync();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // 1x1の白テクスチャを生成
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
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

    // System.Drawing.Common を使用して文字列から Texture2D を生成
    private Texture2D CreateTextTexture(string text, int width, int height)
    {
        using (var bitmap = new Bitmap(width, height))
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(System.Drawing.Color.Transparent);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = TextRenderingHint.AntiAlias;

            using (var font = new Font("Arial", 14, FontStyle.Regular))
            using (var brush = new SolidBrush(System.Drawing.Color.Black))
            {
                // テキストを中央寄せで描画するためのフォーマット設定
                var format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                graphics.DrawString(text, font, brush, new System.Drawing.RectangleF(0, 0, width, height), format);
            }

            // Bitmap を Texture2D に変換
            var texture = new Texture2D(GraphicsDevice, width, height);
            var data = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    data[y * width + x] = new Color(pixel.R, pixel.G, pixel.B, pixel.A);
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

            Color sqColor = Color.LightGray;
            if (square is StartSquare) sqColor = Color.Red;
            else if (square is CheckpointSquare) sqColor = Color.Green;
            else if (square is ShopSquare) sqColor = Color.Blue;
            else if (square is PropertySquare p && p.Owner != null) sqColor = Color.Orange; // 誰かの土地

            // マス本体
            _spriteBatch.Draw(_pixel, rect, sqColor);

            // 枠線っぽく見せるための一回り小さい矩形
            var innerRect = new Rectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4);
            _spriteBatch.Draw(_pixel, innerRect, Color.DarkGray);
        }

        // プレイヤーの描画
        if (_isGameRunning)
        {
            // プレイヤー1と2で位置を少しずらして重ならないようにする
            var players = _gameManager.Players;
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (player.CurrentSquare != null && _squareRects.TryGetValue(player.CurrentSquare, out Rectangle sqRect))
                {
                    int offsetX = (i == 0) ? -15 : 15;
                    int offsetY = (i == 0) ? -15 : 15;
                    var pRect = new Rectangle(sqRect.Center.X + offsetX - 10, sqRect.Center.Y + offsetY - 10, 20, 20);
                    Color pColor = (i == 0) ? Color.Yellow : Color.Cyan;
                    _spriteBatch.Draw(_pixel, pRect, pColor);
                }
            }
        }

        // UI（ボタン）の描画
        foreach (var btn in _currentButtons)
        {
            _spriteBatch.Draw(_pixel, btn.Bounds, btn.Color);
            var innerBtnRect = new Rectangle(btn.Bounds.X + 2, btn.Bounds.Y + 2, btn.Bounds.Width - 4, btn.Bounds.Height - 4);
            _spriteBatch.Draw(_pixel, innerBtnRect, Color.White); // くりぬき

            // テキストテクスチャがあれば描画
            if (btn.TextTexture != null)
            {
                _spriteBatch.Draw(btn.TextTexture, btn.Bounds, Color.White);
            }
        }

        _spriteBatch.End();

        // タイトルバーにメッセージを表示（簡易的な文字表示の代替）
        Window.Title = _currentMessage;

        base.Draw(gameTime);
    }

    // ==========================================
    // 非同期ゲームループとCoreとの連携
    // ==========================================

    private void SetMessage(string message)
    {
        _currentMessage = message;
        Console.WriteLine($"[Game] {message}");
    }

    private async Task GameLoopAsync()
    {
        bool isGameOver = false;
        while (!isGameOver)
        {
            var player = _gameManager.GetCurrentPlayer();
            SetMessage($"=== {player.Name} のターン (所持金:{player.Crystal}) ===");
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
            while (turnActive)
            {
                SetMessage($"{player.Name} のターン: どうしますか？");

                var actionOptions = new List<string> { "サイコロを振る" };
                if (player.CurrentSquare is ShopSquare) actionOptions.Add("ショップを利用");

                int action = await ShowSelectionUIAsync(actionOptions);

                if (action == 0) // サイコロ
                {
                    int roll = _dice.Roll();
                    SetMessage($"サイコロの目: 【 {roll} 】");
                    await Task.Delay(1000);

                    await _gameManager.MovePlayerAsync(player, roll);

                    if (player.CurrentSquare is StartSquare && player.GetVisitedCheckpointCount() == 0 && player.Crystal >= 3000)
                    {
                        // 仮の勝利条件: クリスタル3000以上でスタートに戻る
                        SetMessage($"祝！ {player.Name} が勝利しました！");
                        isGameOver = true;
                    }

                    turnActive = false;
                }
                else if (action == 1) // ショップ
                {
                    if (player.CurrentSquare is ShopSquare shop)
                    {
                        await shop.OnShopEntered(player, shop);
                    }
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
            Texture2D textTex = CreateTextTexture(options[i], btnWidth, btnHeight);

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
                var options = SummonCatalog.AllSummons.Select(s => $"{s.Name} ({s.Cost}C)").ToList();
                options.Insert(0, "戻る");
                SetMessage("どの召喚獣を買いますか？");
                int buyChoice = await ShowSelectionUIAsync(options);

                if (buyChoice > 0)
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
            else if (choice == 2) // 魔法
            {
                var options = MagicCatalog.AllMagics.Select(m => $"{m.Name} ({m.Cost}C)").ToList();
                options.Insert(0, "戻る");
                SetMessage("どの魔法を買いますか？");
                int buyChoice = await ShowSelectionUIAsync(options);

                if (buyChoice > 0)
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
