using System.Collections.Generic;

namespace Game.Core;

public class MapFactory
{
    public Board CreateDefaultMap()
    {
        var board = new Board();

        // ==========================================
        // 1. マスのインスタンスを作成する
        // ==========================================
        
        // スタートマス（StartSquareはShopSquareを継承しているため買い物可能）
        var start = new StartSquare("START", "スタート地点");
        
        // 土地マス（クリスタルを消費してレベルアップや召喚獣配置が可能）
        var propA = new PropertySquare("PROP_A", "平原のマス", 100);
        var propB = new PropertySquare("PROP_B", "森林のマス", 200);
        var propC = new PropertySquare("PROP_C", "火山のマス", 300);
        
        // チェックポイントマス（1〜4のIDを割り当てる）
        var checkpoint1 = new CheckpointSquare("CP_1", "クリスタルポイント1", 1);
        
        // 通常のショップマス
        var shop = new ShopSquare("SHOP_1", "秘密の店");

        // ==========================================
        // 2. マス同士のネットワーク（繋がり）を定義する
        // ==========================================
        
        // スタートからは propA にしか進めない
        start.NextSquares.Add(propA);

        // ★分岐点：propA に止まった後、propB か checkpoint1 の好きな方へ進める
        propA.NextSquares.Add(propB);
        propA.NextSquares.Add(checkpoint1);

        // ルートA（短いルート）: propB -> propC -> スタートに戻る
        propB.NextSquares.Add(propC);
        propC.NextSquares.Add(start);

        // ルートB（チェックポイント・ショップ経由ルート）: checkpoint1 -> shop -> スタートに戻る
        checkpoint1.NextSquares.Add(shop);
        shop.NextSquares.Add(start);

        // ==========================================
        // 3. ボードに登録して完成したマップを返す
        // ==========================================
        
        board.StartSquare = start;
        board.AllSquares.AddRange(new Square[] 
        { 
            start, propA, propB, propC, checkpoint1, shop 
        });

        return board;
    }
}