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

        // ※本来はGameManager等を非同期にする（Taskを返すようにする）対応が必要です
        // 今回は「見た目のUIスクリプト部分」の先行実装となります。
        // （イベントにawait付きのTask処理を登録する場合は、Core側がTask対応している前提となります）

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

                    // 実際にはここでCore側のMovePlayerを呼びます
                    gameManager.MovePlayer(currentPlayer, rollResult);

                    // 最終位置へMoverをジャンプ（同期的なCore移動後の結果描画）
                    await currentMover.MoveToSquareAsync(currentPlayer.CurrentSquare);

                    // 勝利判定
                    if (currentPlayer.CurrentSquare is StartSquare && currentPlayer.GetVisitedCheckpointCount() == 0) // ※仮の勝利条件
                    {
                        // 勝利演出
                    }

                    turnActive = false; // 移動完了でターンエンド
                }
                else if (action == "2") // ショップ
                {
                    if (currentPlayer.CurrentSquare is ShopSquare shop)
                    {
                        // ショップイベントを呼び出す（Core側の実装に依存）
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
}
