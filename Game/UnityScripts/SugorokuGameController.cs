using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Game.Core;

public class SugorokuGameController : MonoBehaviour
{
    [Header("References")]
    public BoardRenderer boardRenderer;
    public SugorokuUIManager uiManager;
    public PlayerMover playerMoverPrefab; // プレイヤーの見た目と移動を管理するプレハブ

    private GameManager gameManager;
    private Dice dice;
    private Dictionary<Player, PlayerMover> playerMovers = new Dictionary<Player, PlayerMover>();

    // アクション選択待機用
    private TaskCompletionSource<string> actionTcs;

    void Start()
    {
        InitializeGame();
        _ = GameLoopAsync();
    }

    private void InitializeGame()
    {
        var mapFactory = new MapFactory();
        var board = mapFactory.CreateDefaultMap();
        gameManager = new GameManager(board);
        dice = new Dice();

        // ボードの生成（3Dモデルの配置）
        boardRenderer.RenderBoard(board);

        // イベントの紐付け
        gameManager.OnRouteSelectionRequested = HandleRouteSelectionAsync;

        // Square側のイベントもUIマネージャー経由で通知できるようにオーバーライド
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

        // プレイヤーの追加
        AddPlayer("プレイヤー1", 1000);
        AddPlayer("プレイヤー2", 1000);

        // UIのアクションボタン紐付け
        uiManager.rollDiceButton.onClick.AddListener(() => OnActionButtonClicked("1"));
        uiManager.shopButton.onClick.AddListener(() => OnActionButtonClicked("2"));
        uiManager.endTurnButton.onClick.AddListener(() => OnActionButtonClicked("0"));
    }

    private void AddPlayer(string name, int crystal)
    {
        var player = new Player(name, crystal);
        gameManager.AddPlayer(player);

        // Unity側のプレイヤー駒（Mover）を生成
        var mover = Instantiate(playerMoverPrefab);
        mover.name = $"Mover_{name}";
        mover.Initialize(player, boardRenderer, gameManager.GameBoard.StartSquare);

        playerMovers[player] = mover;
    }

    private async Task GameLoopAsync()
    {
        bool isGameOver = false;
        while (!isGameOver)
        {
            var currentPlayer = gameManager.GetCurrentPlayer();
            var currentMover = playerMovers[currentPlayer];

            uiManager.UpdateHUD(currentPlayer);
            await uiManager.ShowMessageAsync($"=== {currentPlayer.Name} のターン ===", 1.5f);

            if (currentPlayer.SleepTurns > 0)
            {
                await uiManager.ShowMessageAsync($"{currentPlayer.Name} は眠っている...\n（1回休み）");
                currentPlayer.DecrementStatusEffects();
                gameManager.NextTurn();
                continue;
            }

            bool turnActive = true;
            while (turnActive)
            {
                uiManager.UpdateHUD(currentPlayer);
                uiManager.ShowMessage("どうしますか？");

                // 行動パネルの表示制御
                uiManager.actionPanel.SetActive(true);
                uiManager.shopButton.gameObject.SetActive(currentPlayer.CurrentSquare is ShopSquare);

                // プレイヤーの行動選択を待機
                actionTcs = new TaskCompletionSource<string>();
                string action = await actionTcs.Task;
                uiManager.actionPanel.SetActive(false);

                if (action == "1") // サイコロを振る
                {
                    int rollResult = dice.Roll();
                    await uiManager.ShowMessageAsync($"サイコロの目: 【 {rollResult} 】");

                    await gameManager.MovePlayerAsync(currentPlayer, rollResult);

                    // 最終位置へMoverを移動
                    await currentMover.MoveToSquareAsync(currentPlayer.CurrentSquare);

                    // 勝利判定
                    if (currentPlayer.CurrentSquare is StartSquare && currentPlayer.GetVisitedCheckpointCount() == 0) // ※仮の勝利条件
                    {
                        // await uiManager.ShowMessageAsync($"祝！ {currentPlayer.Name}が帰還しました！", 3.0f);
                        // isGameOver = true;
                    }

                    turnActive = false; // 移動完了でターンエンド
                }
                else if (action == "2") // ショップ
                {
                    if (currentPlayer.CurrentSquare is ShopSquare shop)
                    {
                        await shop.OnShopEntered(currentPlayer, shop);
                    }
                }
                else if (action == "0") // ターン終了
                {
                    turnActive = false;
                }
            }

            gameManager.NextTurn();
        }
    }

    private void OnActionButtonClicked(string actionType)
    {
        if (actionTcs != null && !actionTcs.Task.IsCompleted)
        {
            actionTcs.SetResult(actionType);
        }
    }

    // --- Coreイベントのハンドラ群（UIを呼び出して待機する） ---

    private async Task<Square> HandleRouteSelectionAsync(Player player, List<Square> availableSquares)
    {
        var options = availableSquares.Select(s => s.Name).ToList();
        int selectedIndex = await uiManager.WaitForSelectionAsync("進むルートを選択してください", options);

        await playerMovers[player].MoveToSquareAsync(availableSquares[selectedIndex]);

        return availableSquares[selectedIndex];
    }

    private async Task HandlePurchaseRequestAsync(Player player, PropertySquare prop)
    {
        if (player.Inventory.Summons.Count == 0)
        {
            await uiManager.ShowMessageAsync($"空き地『{prop.Name}』に到着したが、配置できる召喚獣を持っていない...");
            return;
        }

        var options = player.Inventory.Summons.Select(s => $"{s.Name} (倍率: {s.TollMultiplier})").ToList();
        options.Insert(0, "やめる"); // 0番目をキャンセルとする

        int choiceIndex = await uiManager.WaitForSelectionAsync($"『{prop.Name}』({prop.BaseValue} C) を獲得しますか？", options);

        if (choiceIndex > 0) // やめる以外を選択
        {
            var targetSummon = player.Inventory.Summons[choiceIndex - 1];
            if (prop.TryBuy(player, targetSummon))
            {
                player.Inventory.RemoveSummon(targetSummon);
                uiManager.UpdateHUD(player);
                await uiManager.ShowMessageAsync($"{prop.Name} を購入し、\n{targetSummon.Name} を配置しました！");
            }
            else
            {
                await uiManager.ShowMessageAsync("クリスタルが足りません！");
            }
        }
    }

    private async Task HandleUpgradeRequestAsync(Player player, PropertySquare prop)
    {
        int upgradeCost = prop.BaseValue * prop.Level;
        var options = new List<string> { "増資する", "やめる" };

        int choice = await uiManager.WaitForSelectionAsync($"自領地『{prop.Name}』(Lv {prop.Level})\n{upgradeCost}C で増資しますか？", options);

        if (choice == 0) // 増資する
        {
            if (prop.TryUpgrade(player))
            {
                uiManager.UpdateHUD(player);
                await uiManager.ShowMessageAsync($"{prop.Name} が Lv{prop.Level} になりました！");
            }
            else
            {
                await uiManager.ShowMessageAsync("クリスタルが足りません...");
            }
        }
    }

    private async Task HandleTollPaidAsync(Player player, PropertySquare prop, int amount)
    {
        string msg = $"{prop.Owner.Name} の領地『{prop.Name}』だ！\n{amount} クリスタル支払った...";
        if (prop.PlacedSummon != null)
        {
            msg = $"召喚獣 {prop.PlacedSummon.Name} が立ちはだかる！\n" + msg;
        }
        uiManager.UpdateHUD(player);
        await uiManager.ShowMessageAsync(msg, 2.5f);
    }

    private async Task HandleShopEnteredAsync(Player player, ShopSquare shop)
    {
        bool inShop = true;
        while (inShop)
        {
            uiManager.UpdateHUD(player);
            int choice = await uiManager.WaitForSelectionAsync("【ショップ】\n何を買いますか？", new List<string> { "出る", "召喚獣", "魔法" });

            if (choice == 0)
            {
                inShop = false;
            }
            else if (choice == 1) // 召喚獣
            {
                var options = SummonCatalog.AllSummons.Select(s => $"{s.Name} ({s.Cost}C)").ToList();
                options.Insert(0, "戻る");
                int buyChoice = await uiManager.WaitForSelectionAsync("どの召喚獣を買いますか？", options);

                if (buyChoice > 0)
                {
                    var target = SummonCatalog.AllSummons[buyChoice - 1];
                    if (shop.TryBuySummon(player, target))
                    {
                        await uiManager.ShowMessageAsync($"{target.Name} を購入しました！");
                    }
                    else
                    {
                        await uiManager.ShowMessageAsync("クリスタル不足か、枠がいっぱいです。");
                    }
                }
            }
            else if (choice == 2) // 魔法
            {
                var options = MagicCatalog.AllMagics.Select(m => $"{m.Name} ({m.Cost}C)").ToList();
                options.Insert(0, "戻る");
                int buyChoice = await uiManager.WaitForSelectionAsync("どの魔法を買いますか？", options);

                if (buyChoice > 0)
                {
                    var target = MagicCatalog.AllMagics[buyChoice - 1];
                    if (shop.TryBuyMagic(player, target))
                    {
                        await uiManager.ShowMessageAsync($"{target.Name} を購入しました！");
                    }
                    else
                    {
                        await uiManager.ShowMessageAsync("クリスタル不足か、枠がいっぱいです。");
                    }
                }
            }
        }
    }

    private async Task HandleBonusAwardedAsync(Player player, int amount, int count)
    {
        string msg = $"チェックポイントを {count} 個集めて帰還！\n報酬 {amount} クリスタルを獲得！";
        if (count == 4) msg = "★パーフェクトボーナス！★\n" + msg;

        uiManager.UpdateHUD(player);
        await uiManager.ShowMessageAsync(msg, 2.5f);
    }
}
