using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Game.Core;

// ※このスクリプトはUnityプロジェクト側にインポートして使用する想定のものです。
// C#プロジェクト(Game/Core, Game/UI)自体のビルドには含めません。

public class SugorokuUIController : MonoBehaviour
{
    [Header("UI References")]
    public Text messageText;
    public Button rollDiceButton;
    public Button option1Button;
    public Button option2Button;
    public Text option1Text;
    public Text option2Text;

    private GameManager gameManager;
    private Dice dice;
    private TaskCompletionSource<int> diceRollTcs;
    private TaskCompletionSource<Square> routeSelectionTcs;

    void Start()
    {
        InitializeGame();

        // 最初のターンを開始
        _ = GameLoopAsync();
    }

    private void InitializeGame()
    {
        var mapFactory = new MapFactory();
        var board = mapFactory.CreateDefaultMap();
        gameManager = new GameManager(board);
        dice = new Dice();

        var player1 = new Player("プレイヤー1", 1000);
        gameManager.AddPlayer(player1);

        // ルート選択デリゲートの登録 (非同期Taskを返す)
        gameManager.OnRouteSelectionRequested = HandleRouteSelectionAsync;

        // ボタンイベントの紐付け
        rollDiceButton.onClick.AddListener(OnRollDiceButtonClicked);
        option1Button.onClick.AddListener(() => OnOptionButtonClicked(0));
        option2Button.onClick.AddListener(() => OnOptionButtonClicked(1));

        HideOptions();
    }

    private async Task GameLoopAsync()
    {
        while (true)
        {
            var player = gameManager.GetCurrentPlayer();
            messageText.text = $"{player.Name} のターン。\nサイコロを振ってください。";

            // サイコロが振られるのを待つ
            rollDiceButton.interactable = true;
            diceRollTcs = new TaskCompletionSource<int>();
            int roll = await diceRollTcs.Task;

            messageText.text = $"{roll} が出た！";

            // 移動処理（awaitすることでルート選択などの非同期処理を待つ）
            await gameManager.MovePlayerAsync(player, roll);

            messageText.text = $"{player.Name} は {player.CurrentSquare.Name} に止まった！";

            // 一定時間待ってから次のターンへ
            await Task.Delay(2000);
            gameManager.NextTurn();
        }
    }

    private void OnRollDiceButtonClicked()
    {
        rollDiceButton.interactable = false;
        if (diceRollTcs != null && !diceRollTcs.Task.IsCompleted)
        {
            diceRollTcs.SetResult(dice.Roll());
        }
    }

    private async Task<Square> HandleRouteSelectionAsync(Player player, List<Square> availableSquares)
    {
        messageText.text = "進むルートを選択してください！";

        // 分岐先の選択肢を表示
        if (availableSquares.Count >= 1)
        {
            option1Text.text = availableSquares[0].Name;
            option1Button.gameObject.SetActive(true);
        }
        if (availableSquares.Count >= 2)
        {
            option2Text.text = availableSquares[1].Name;
            option2Button.gameObject.SetActive(true);
        }

        // プレイヤーの選択を待つ
        routeSelectionTcs = new TaskCompletionSource<Square>();
        Square selected = await routeSelectionTcs.Task;

        HideOptions();
        return selected;
    }

    private void OnOptionButtonClicked(int index)
    {
        var player = gameManager.GetCurrentPlayer();
        var available = player.CurrentSquare.NextSquares;

        if (index < available.Count && routeSelectionTcs != null && !routeSelectionTcs.Task.IsCompleted)
        {
            routeSelectionTcs.SetResult(available[index]);
        }
    }

    private void HideOptions()
    {
        option1Button.gameObject.SetActive(false);
        option2Button.gameObject.SetActive(false);
    }
}
