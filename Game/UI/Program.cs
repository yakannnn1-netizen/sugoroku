using System;
using System.Collections.Generic;
using Game.Core;

Console.WriteLine("=== スゴロクゲーム 開始 ===");

// 1. マップとゲームマネージャーの初期化
var mapFactory = new MapFactory();
var board = mapFactory.CreateDefaultMap();
var gameManager = new GameManager(board);
var dice = new Dice();

// 2. プレイヤーの登録
var player1 = new Player("プレイヤー1", 1000);
var player2 = new Player("プレイヤー2", 1000);
gameManager.AddPlayer(player1);
gameManager.AddPlayer(player2);

// 3. 分岐点でのルート選択処理（オートプレイ）
gameManager.OnRouteSelectionRequested = (player, availableSquares) =>
{
    Console.WriteLine($"\n【分岐点】 {player.Name}さん、進むルートを選択してください。（オート選択: {availableSquares[0].Name}）");
    return System.Threading.Tasks.Task.FromResult(availableSquares[0]);
};

foreach (var square in board.AllSquares)
{
    if (square is PropertySquare propertySquare)
    {
        // オートプレイ（今回は何もしない）
        propertySquare.OnPurchaseRequested = (player, prop) =>
        {
            Console.WriteLine($"\n【土地獲得】 空き地『{prop.Name}』に到着しました。(価格: {prop.BaseValue}クリスタル)");
            Console.WriteLine("オートプレイ中のため土地の獲得は見送ります。");
            return System.Threading.Tasks.Task.CompletedTask;
        };

        propertySquare.OnUpgradeRequested = (player, prop) =>
        {
            Console.WriteLine($"\n【自領地到着】 自分の領地『{prop.Name}』に到着しました。");
            Console.WriteLine("オートプレイ中のため増資は見送ります。");
            return System.Threading.Tasks.Task.CompletedTask;
        };

        // 他人の土地に止まり、通行料を支払った時のUI
        propertySquare.OnTollPaid = (player, prop, amount) =>
        {
            Console.WriteLine($"\n【徴収発生！】 {prop.Owner.Name} の領地『{prop.Name}』に止まってしまった！");
            if (prop.PlacedSummon != null)
            {
                Console.WriteLine($"召喚獣 {prop.PlacedSummon.Name} が睨みを利かせている…！");
            }
            Console.WriteLine($"{amount} クリスタルを支払いました。(残りクリスタル: {player.Crystal})");
            return System.Threading.Tasks.Task.CompletedTask;
        };
    }
    else if (square is ShopSquare shopSquare)
    {
        shopSquare.OnShopEntered = (player, shop) =>
        {
            Console.WriteLine($"\n【ショップ】いらっしゃいませ！ (所持クリスタル: {player.Crystal})");
            Console.WriteLine("オートプレイ中のため買い物をスキップします。");
            return System.Threading.Tasks.Task.CompletedTask;
        };
    }
    else if (square is StartSquare startSquare)
    {
        startSquare.OnBonusAwarded = (player, amount, count) =>
        {
            Console.WriteLine($"\n--- 【スタート精算フェーズ】 ---");
            Console.WriteLine($"{player.Name} はチェックポイントを 【 {count} / 4 】 個集めて帰還しました！");
            
            if (count == 4)
            {
                Console.WriteLine($"★★★ 見事パーフェクト達成！特大ボーナス！ ★★★");
            }
            else
            {
                Console.WriteLine($"地道な周回ボーナスを獲得！");
            }

            Console.WriteLine($"報酬として {amount} クリスタルを獲得！ (現在のクリスタル: {player.Crystal})");
            return System.Threading.Tasks.Task.CompletedTask;
        };
    }
}

// 4. メインループ（ターン制の進行・オートプレイ）
bool isGameOver = false;
int turnCount = 0;
int maxTurns = 20; // テスト用に20ターンで終了

while (!isGameOver && turnCount < maxTurns)
{
    var currentPlayer = gameManager.GetCurrentPlayer();
    
    Console.WriteLine($"\n========================================");
    Console.WriteLine($"--- {currentPlayer.Name} のターン (Turn {turnCount+1}) ---");
    Console.WriteLine($"現在地: {currentPlayer.CurrentSquare.Name} / 所持クリスタル: {currentPlayer.Crystal}");
    Console.WriteLine($"========================================");
    
    if (currentPlayer.SealTurns > 0) Console.WriteLine($"【状態異常】 封印状態（あと {currentPlayer.SealTurns} ターン土地の購入・増資不可）");

    if (currentPlayer.SleepTurns > 0)
    {
        Console.WriteLine($"{currentPlayer.Name} は眠っている...（1回休み）");
        currentPlayer.DecrementStatusEffects(); // ターンを消費
        gameManager.NextTurn();
        turnCount++;
        continue; // 次の人のターンへスキップ
    }
    
    // サイコロを振る
    int rollResult = dice.Roll();
    Console.WriteLine($"サイコロを振って 【 {rollResult} 】 が出た！");

    // Core側に移動処理をお任せする（非同期のTaskを待機）
    await gameManager.MovePlayerAsync(currentPlayer, rollResult);

    // 今回は暫定的な勝利判定（チェックポイントをすべて回り、スタート地点にいるか等）
    if (currentPlayer.CurrentSquare is StartSquare && currentPlayer.GetVisitedCheckpointCount() == 0 && turnCount > 5)
    {
        Console.WriteLine($"\n祝！ {currentPlayer.Name}がすべてのチェックポイントを回り帰還しました！");
        isGameOver = true;
        break;
    }

    // 次の人へターンを回す
    gameManager.NextTurn();
    turnCount++;
}

Console.WriteLine("ゲーム終了");