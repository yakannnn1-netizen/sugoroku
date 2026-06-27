namespace Game.Core;

public class StartSquare : ShopSquare
{
    public Action<Player, int, int> OnBonusAwarded { get; set; }

    public StartSquare(string id, string name) : base(id, name) { }

    public override void OnLanded(Player player)
    {
        CheckAndAwardBonus(player);
        // 止まった時は親クラス(ShopSquare)の処理でショップが呼ばれる
        base.OnLanded(player);
    }
    
    public override void OnPassed(Player player)
    {
        CheckAndAwardBonus(player);
        // 通過時にもショップ機能を呼び出す
        OnShopEntered?.Invoke(player, this);
    }

    private void CheckAndAwardBonus(Player player)
    {
        int count = player.GetVisitedCheckpointCount();

        // 1つもチェックポイントを通っていない場合はボーナスなし
        if (count == 0) return;

        // ★所持数に応じたボーナス額の計算（4個で一気に跳ね上がる傾斜）
        int bonusAmount = count switch
        {
            1 => 100,   // 1個：ちょっとしたお小遣い
            2 => 300,   // 2個：少し色を付ける
            3 => 600,   // 3個：堅実な稼ぎ
            4 => 2000,  // 4個（全制覇）：大ボーナス！
            _ => 0
        };

        player.AddCrystal(bonusAmount);
        player.ResetCheckpoints(); // フラグをリセットして次の周回へ
        
        OnBonusAwarded?.Invoke(player, bonusAmount, count);
    }
}