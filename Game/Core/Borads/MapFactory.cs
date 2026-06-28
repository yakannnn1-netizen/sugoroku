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
        var propD = new PropertySquare("PROP_D", "砂漠のマス", 400);
        var propE = new PropertySquare("PROP_E", "雪原のマス", 500);
        var propF = new PropertySquare("PROP_F", "深海のマス", 600);
        var propG = new PropertySquare("PROP_G", "天空のマス", 700);
        
        // チェックポイントマス（1〜4のIDを割り当てる）
        var cp1 = new CheckpointSquare("CP_1", "ポイント1", 1);
        var cp2 = new CheckpointSquare("CP_2", "ポイント2", 2);
        var cp3 = new CheckpointSquare("CP_3", "ポイント3", 3);
        var cp4 = new CheckpointSquare("CP_4", "ポイント4", 4);
        
        // 通常のショップマス
        var shop1 = new ShopSquare("SHOP_1", "西の店");
        var shop2 = new ShopSquare("SHOP_2", "東の店");

        // ==========================================
        // 2. マス同士のネットワーク（繋がり）を定義する
        // ==========================================
        
        // 1つの大きな周回ルートを形成
        start.NextSquares.Add(propA);
        propA.NextSquares.Add(cp1);
        cp1.NextSquares.Add(propB);
        propB.NextSquares.Add(shop1);
        shop1.NextSquares.Add(propC);
        propC.NextSquares.Add(cp2);

        cp2.NextSquares.Add(propD);
        propD.NextSquares.Add(propE);

        // 分岐点
        propE.NextSquares.Add(cp3);
        propE.NextSquares.Add(shop2);

        // ルートA
        cp3.NextSquares.Add(propF);
        propF.NextSquares.Add(cp4);

        // ルートB
        shop2.NextSquares.Add(propG);
        propG.NextSquares.Add(cp4);

        // 合流してスタートへ戻る
        cp4.NextSquares.Add(start);

        // ==========================================
        // 3. ボードに登録して完成したマップを返す
        // ==========================================
        
        board.StartSquare = start;
        board.AllSquares.AddRange(new Square[] 
        { 
            start, propA, cp1, propB, shop1, propC, cp2, propD, propE, cp3, shop2, propF, propG, cp4
        });

        return board;
    }
}