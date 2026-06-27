namespace Game.Core;

public class ShopSquare : Square
{
    public Func<Player, ShopSquare, System.Threading.Tasks.Task> OnShopEntered { get; set; }
    public ShopSquare(string id, string name) : base(id, name) { }

    public override async System.Threading.Tasks.Task OnLandedAsync(Player player)
    {
        // 買い物フェーズへの移行トリガーとして利用します
        if (OnShopEntered != null)
        {
            await OnShopEntered(player, this);
        }
    }

    public bool TryBuySummon(Player player, Summon target)
    {
        // クリスタルが足りていて、かつインベントリに空きがあるか確認
        if (player.Crystal >= target.Cost && player.Inventory.TryAddSummon(target))
        {
            player.AddCrystal(-target.Cost);
            return true; // 購入成功
        }
        return false; // 購入失敗
    }

    public bool TryBuyMagic(Player player, Magic target)
    {
        if (player.Crystal >= target.Cost && player.Inventory.TryAddMagic(target))
        {
            player.AddCrystal(-target.Cost);
            return true;
        }
        return false;
    }
}