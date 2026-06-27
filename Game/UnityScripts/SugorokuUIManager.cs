using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Game.Core;

public class SugorokuUIManager : MonoBehaviour
{
    [Header("Main Panels")]
    public GameObject messageWindow;
    public Text messageText;

    [Header("Action Panel")]
    public GameObject actionPanel;
    public Button rollDiceButton;
    public Button shopButton;
    public Button endTurnButton;

    [Header("Selection Panel (Route / Items)")]
    public GameObject selectionPanel;
    public Button[] selectionButtons; // 汎用的な選択ボタンの配列
    public Text[] selectionTexts;

    [Header("Status HUD")]
    public Text crystalText;
    public Text playerNameText;
    public Text statusEffectText;

    private TaskCompletionSource<int> currentSelectionTcs;

    private void Awake()
    {
        HideAll();

        // ボタンへのイベント紐付け
        for (int i = 0; i < selectionButtons.Length; i++)
        {
            int index = i; // クロージャ用
            selectionButtons[i].onClick.AddListener(() => OnSelectionButtonClicked(index));
        }
    }

    public void UpdateHUD(Player player)
    {
        playerNameText.text = player.Name;
        crystalText.text = $"Crystal: {player.Crystal}";

        if (player.SealTurns > 0) statusEffectText.text = $"【封印】あと{player.SealTurns}T";
        else if (player.SleepTurns > 0) statusEffectText.text = $"【睡眠】";
        else statusEffectText.text = "";
    }

    public async Task ShowMessageAsync(string message, float duration = 2.0f)
    {
        messageWindow.SetActive(true);
        messageText.text = message;
        await Task.Delay((int)(duration * 1000));
        messageWindow.SetActive(false);
    }

    public void ShowMessage(string message)
    {
        messageWindow.SetActive(true);
        messageText.text = message;
    }

    public void HideMessage()
    {
        messageWindow.SetActive(false);
    }

    // --- ルート選択やアイテム選択用の汎用待機メソッド ---
    public Task<int> WaitForSelectionAsync(string prompt, List<string> options)
    {
        ShowMessage(prompt);
        selectionPanel.SetActive(true);

        for (int i = 0; i < selectionButtons.Length; i++)
        {
            if (i < options.Count)
            {
                selectionButtons[i].gameObject.SetActive(true);
                selectionTexts[i].text = options[i];
            }
            else
            {
                selectionButtons[i].gameObject.SetActive(false);
            }
        }

        currentSelectionTcs = new TaskCompletionSource<int>();
        return currentSelectionTcs.Task;
    }

    private void OnSelectionButtonClicked(int index)
    {
        if (currentSelectionTcs != null && !currentSelectionTcs.Task.IsCompleted)
        {
            selectionPanel.SetActive(false);
            HideMessage();
            currentSelectionTcs.SetResult(index);
        }
    }

    public void HideAll()
    {
        messageWindow.SetActive(false);
        actionPanel.SetActive(false);
        selectionPanel.SetActive(false);
    }
}
