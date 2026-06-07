using System.Collections.Generic;

namespace Game.Core;

public class Board
{
    // ゲームの起点となるスタートマス（兼ショップ）
    public StartSquare StartSquare { get; set; }

    // マップ上の全マスを保持するリスト（マス数や全体状況の把握に使用）
    public List<Square> AllSquares { get; } = new List<Square>();

    // 特定のIDからマスを検索する便利メソッド
    public Square GetSquareById(string id)
    {
        return AllSquares.Find(square => square.Id == id);
    }
}