using System;
using System.Collections.Generic;

namespace Game.Core;

public class GameManager
{
    public Board GameBoard { get; }
    public List<Player> Players { get; } = new List<Player>();
    public int CurrentPlayerIndex { get; private set; } = 0;

    // ★重要: UI側にルート選択を要求するための「デリゲート（関数の型）」を定義
    // 「プレイヤーと選択肢のリストを渡すから、選んだマスを返してね」という契約です
    public delegate Square RouteSelectionHandler(Player player, List<Square> availableSquares);
    
    // UI側からここに実際の選択処理（メソッド）をセットしてもらいます
    public RouteSelectionHandler OnRouteSelectionRequested { get; set; }

    public GameManager(Board board)
    {
        GameBoard = board;
    }

    public void AddPlayer(Player player)
    {
        // 参加時にスタートマスに配置する
        player.CurrentSquare = GameBoard.StartSquare;
        Players.Add(player);
    }

    public Player GetCurrentPlayer()
    {
        return Players[CurrentPlayerIndex];
    }

    // 次のプレイヤーにターンを回す
    public void NextTurn()
    {
        CurrentPlayerIndex = (CurrentPlayerIndex + 1) % Players.Count;
    }

    // プレイヤーを指定した歩数だけ進めるメインロジック
    public void MovePlayer(Player player, int steps)
    {
        int remainingSteps = steps;

        while (remainingSteps > 0)
        {
            var nextSquares = player.CurrentSquare.NextSquares;

            if (nextSquares.Count == 0)
            {
                // 行き止まり（通常マップの設計ミスですが安全のため）
                break;
            }
            else if (nextSquares.Count == 1)
            {
                // 一本道なら自動で進む
                player.CurrentSquare = nextSquares[0];
            }
            else
            {
                // ★分岐点に到達！UI側に選択を委譲する
                if (OnRouteSelectionRequested == null)
                {
                    throw new InvalidOperationException("UI側のルート選択処理がセットされていません。");
                }

                // UI側で選ばれたマスを受け取って現在地を更新する
                Square selectedSquare = OnRouteSelectionRequested(player, nextSquares);
                player.CurrentSquare = selectedSquare;
            }

            remainingSteps--;

            // まだ移動中（最後の一歩ではない）なら、通過処理を実行する
            if (remainingSteps > 0)
            {
                player.CurrentSquare.OnPassed(player);
            }
        }

        // 歩き終わったら、最終的に止まったマスのイベントを実行する
        player.CurrentSquare.OnLanded(player);
    }
}