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

// 3. 【最重要】分岐点でのルート選択処理（UIの実装）をManagerに渡す
gameManager.OnRouteSelectionRequested = (player, availableSquares) =>
{
    Console.WriteLine($"\n【分岐点】 {player.Name}さん、進むルートを選択してください。");
    
    // 選択肢を表示
    for (int i = 0; i < availableSquares.Count; i++)
    {
        Console.WriteLine($"{i + 1}: {availableSquares[i].Name}");
    }

    // 正しい入力がされるまでループ
    while (true)
    {
        Console.Write("番号を入力: ");
        string input = Console.ReadLine();
        
        if (int.TryParse(input, out int choice) && choice >= 1 && choice <= availableSquares.Count)
        {
            // 選ばれたマスをCore側（GameManager）に返す！
            return availableSquares[choice - 1];
        }
        Console.WriteLine("正しい番号を入力してください。");
    }
};

foreach (var square in board.AllSquares)
{
    if (square is PropertySquare propertySquare)
    {
        // 未所有の土地に止まった時のUI
        propertySquare.OnPurchaseRequested = (player, prop) =>
        {
            Console.WriteLine($"\n【土地獲得】 空き地『{prop.Name}』に到着しました。(価格: {prop.BaseValue}クリスタル)");
            
            // ★必須条件: インベントリに召喚獣がいるかチェック
            if (player.Inventory.Summons.Count == 0)
            {
                Console.WriteLine("しかし、配置できる召喚獣を持っていないため、この土地は獲得できません！");
                return; // 処理を終了して通過させる
            }

            Console.WriteLine($"あなたの所持クリスタル: {player.Crystal}");
            Console.WriteLine("この土地を獲得するために配置する召喚獣を選んでください（キャンセルは0）:");
            
            // 所持している召喚獣をリストアップ
            for (int i = 0; i < player.Inventory.Summons.Count; i++)
            {
                var s = player.Inventory.Summons[i];
                Console.WriteLine($"{i + 1}: {s.Name} (徴収倍率: {s.TollMultiplier}倍)");
            }

            Console.Write("選択: ");
            if (int.TryParse(Console.ReadLine(), out int choice) && choice > 0 && choice <= player.Inventory.Summons.Count)
            {
                var targetSummon = player.Inventory.Summons[choice - 1];
                
                // Core側の購入（設置）ロジックを呼び出す
                if (prop.TryBuy(player, targetSummon))
                {
                    // 成功したらインベントリから消費する
                    player.Inventory.RemoveSummon(targetSummon);
                    Console.WriteLine($"{prop.Name} を購入し、{targetSummon.Name} を配置しました！");
                    Console.WriteLine($"(残りクリスタル: {player.Crystal})");
                }
                else
                {
                    Console.WriteLine("クリスタルが足りません！");
                }
            }
            else
            {
                Console.WriteLine("土地の獲得を見送りました。");
            }
        };

        propertySquare.OnUpgradeRequested = (player, prop) =>
        {
            int upgradeCost = prop.BaseValue * prop.Level;
            Console.WriteLine($"\n【自領地到着】 自分の領地『{prop.Name}』に到着しました。");
            Console.WriteLine($"現在のレベル: {prop.Level} / 次のレベルへの増資費用: {upgradeCost}クリスタル");
            Console.WriteLine($"あなたの所持クリスタル: {player.Crystal}");
            Console.Write("増資してレベルアップしますか？ (y/n): ");

            var input = Console.ReadLine();
            if (input?.ToLower() == "y")
            {
                if (prop.TryUpgrade(player))
                {
                    Console.WriteLine($"増資成功！ {prop.Name} が レベル{prop.Level} にアップグレードされました！");
                    Console.WriteLine($"(残りクリスタル: {player.Crystal})");
                }
                else
                {
                    Console.WriteLine("クリスタルが足りません！今回はゆっくり休んでいきます。");
                }
            }
            else
            {
                Console.WriteLine("今回は増資せず、ゆっくり休んでいきます。");
            }
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
        };
    }
    else if (square is ShopSquare shopSquare)
    {
        shopSquare.OnShopEntered = (player, shop) =>
        {
            while (true)
            {
                Console.WriteLine($"\n【ショップ】いらっしゃいませ！ (所持クリスタル: {player.Crystal})");
                Console.WriteLine("1: 召喚獣を買う / 2: 魔法を買う / 0: 出る");
                Console.Write("選択: ");
                var shopInput = Console.ReadLine();

                if (shopInput == "0")
                {
                    Console.WriteLine("ショップを出ました。");
                    break; // ループを抜けて盤面に戻る
                }
                else if (shopInput == "1")
                {
                    // ... 召喚獣リストの表示処理 ...
                    Console.Write("購入する番号 (キャンセルは0): ");
                    if (int.TryParse(Console.ReadLine(), out int choice) && choice > 0 && choice <= SummonCatalog.AllSummons.Count)
                    {
                        var target = SummonCatalog.AllSummons[choice - 1];
                        
                        // ★購入の判定自体は ShopSquare(Core側) に任せる！
                        if (shop.TryBuySummon(player, target))
                        {
                            Console.WriteLine($"{target.Name} を購入し、インベントリに追加しました！");
                        }
                        else
                        {
                            Console.WriteLine("クリスタルが足りないか、所持枠がいっぱいです！");
                        }
                    }
                }
                else if (shopInput == "2")
                {
                    // ... 魔法リストの表示処理 ...
                    Console.Write("購入する番号 (キャンセルは0): ");
                    if (int.TryParse(Console.ReadLine(), out int choice) && choice > 0 && choice <= MagicCatalog.AllMagics.Count)
                    {
                        var target = MagicCatalog.AllMagics[choice - 1];

                        // ★購入の判定自体は ShopSquare(Core側) に任せる！
                        if (shop.TryBuyMagic(player, target))
                        {
                            Console.WriteLine($"{target.Name} を購入し、インベントリに追加しました！");
                        }
                        else
                        {
                            Console.WriteLine("クリスタルが足りないか、所持枠がいっぱいです！");
                        }
                    }
                }
            }
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
        };
    }
}

// 4. メインループ（ターン制の進行）
bool isGameOver = false;
while (!isGameOver)
{
    var currentPlayer = gameManager.GetCurrentPlayer();
    
    Console.WriteLine($"\n========================================");
    Console.WriteLine($"--- {currentPlayer.Name} のターン ---");
    Console.WriteLine($"現在地: {currentPlayer.CurrentSquare.Name} / 所持クリスタル: {currentPlayer.Crystal}");
    Console.WriteLine($"========================================");
    
    if (currentPlayer.SealTurns > 0) Console.WriteLine($"【状態異常】 封印状態（あと {currentPlayer.SealTurns} ターン土地の購入・増資不可）");

    if (currentPlayer.SleepTurns > 0)
    {
        Console.WriteLine($"{currentPlayer.Name} は眠っている...（1回休み）");
        currentPlayer.DecrementStatusEffects(); // ターンを消費
        gameManager.NextTurn();
        continue; // 次の人のターンへスキップ
    }
    
    Console.WriteLine("\nどうしますか？");
    Console.WriteLine("1: サイコロを振る");
    
    // スタート地点（ShopSquare）にいる場合は買い物ができる
    if (currentPlayer.CurrentSquare is ShopSquare shop)
    {
        Console.WriteLine("2: ショップを利用する");
    }
    Console.WriteLine("0: ゲーム終了");

    Console.Write("選択: ");
    var actionInput = Console.ReadLine();

    if (actionInput == "1")
    {
        break; // ループを抜けて移動処理（サイコロ）へ進む
    }
    else if (actionInput == "2" && currentPlayer.CurrentSquare is ShopSquare currentShop)
    {
        // Core側の処理は使わず、初期化時に設定した同じショップUIをここで再利用して呼び出す
        currentShop.OnShopEntered?.Invoke(currentPlayer, currentShop);
    }
    else
    {
        Console.WriteLine("正しい番号を入力してください。");
    }
    var input = Console.ReadLine();
    if (input == "0") break;

    // サイコロを振る
    int rollResult = dice.Roll();
    Console.WriteLine($"サイコロの目: 【 {rollResult} 】");

    // Core側に移動処理をお任せする（分岐があれば勝手に手順3の処理が呼ばれる）
    gameManager.MovePlayer(currentPlayer, rollResult);

    // 今回は暫定的な勝利判定（チェックポイントをすべて回り、スタート地点にいるか等）
    // ※後ほど「クリスタルが一定以上」などの正式な勝利条件に書き換えます
    if (currentPlayer.CurrentSquare is StartSquare)
    {
        Console.WriteLine($"\n祝！ {currentPlayer.Name}がすべてのチェックポイントを回り帰還しました！");
        isGameOver = true;
        break;
    }

    // 次の人へターンを回す
    gameManager.NextTurn();
}

Console.WriteLine("ゲーム終了");