namespace Game.Core;

public class StartSquare : ShopSquare
{
    public Func<Player, int, int, System.Threading.Tasks.Task> OnBonusAwarded { get; set; }

    public StartSquare(string id, string name) : base(id, name) { }

    public override async System.Threading.Tasks.Task OnLandedAsync(Player player)
    {
        await CheckAndAwardBonus(player);
        // 止まった時は親クラス(ShopSquare)の処理でショップが呼ばれる
        await base.OnLandedAsync(player);
    }
    
    public override async System.Threading.Tasks.Task OnPassedAsync(Player player)
    {
        await CheckAndAwardBonus(player);
        // 通過時にもショップ機能を呼び出す
        if (OnShopEntered != null)
        {
            await OnShopEntered(player, this);
        }
    }

    private async System.Threading.Tasks.Task CheckAndAwardBonus(Player player)
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
        
        if (OnBonusAwarded != null)
        {
            await OnBonusAwarded(player, bonusAmount, count);
        }
    }
}