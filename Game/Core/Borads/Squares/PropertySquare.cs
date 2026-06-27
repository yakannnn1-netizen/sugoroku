namespace Game.Core;

public class PropertySquare : Square
{
    public int BaseValue { get; } // 基礎価格
    public int Level { get; private set; } = 1; // 現在のレベル
    public Player Owner { get; private set; } // 所有者（nullなら未購入）
    public Summon PlacedSummon { get; private set; } // 配置された召喚獣

    public Func<Player, PropertySquare, System.Threading.Tasks.Task> OnPurchaseRequested { get; set; }
    public Func<Player, PropertySquare, int, System.Threading.Tasks.Task> OnTollPaid { get; set; }
    public Func<Player, PropertySquare, System.Threading.Tasks.Task> OnUpgradeRequested { get; set; }

    public PropertySquare(string id, string name, int baseValue) : base(id, name)
    {
        BaseValue = baseValue;
    }

    public void SetOwner(Player player)
    {
        Owner = player;
    }

    public int CalculateToll()
    {
        int toll = BaseValue * Level;
        if (PlacedSummon != null)
        {
            toll = (int)(toll * PlacedSummon.TollMultiplier);
        }
        return toll;
    }

    public override async System.Threading.Tasks.Task OnLandedAsync(Player player)
    {
        if (Owner == null)
        {
            // パターン1: 未購入の土地
            if (OnPurchaseRequested != null)
            {
                await OnPurchaseRequested(player, this);
            }
        }
        else if (Owner != player)
        {
            // パターン2: 他人の土地（徴収処理）
            int toll = CalculateToll();
            
            // 払えるだけ払う（破産処理は一旦シンプル化し、所持金が足りなければ全額没収）
            int actualPayment = player.Crystal >= toll ? toll : player.Crystal;
            
            player.AddCrystal(-actualPayment);
            Owner.AddCrystal(actualPayment);

            // UI側に「いくら払ったか」を通知する
            if (OnTollPaid != null)
            {
                await OnTollPaid(player, this, actualPayment);
            }

            // 召喚獣が配置されていれば、特殊能力を発動！
            PlacedSummon?.OnEnemyLanded(player);
        }
        else
        {
            // パターン3: 自分の土地（増資や召喚獣の配置など。今回は一旦UI側に委譲しなくても良いようシンプルに維持）
            Console.WriteLine($"\nここは {player.Name} の領地だ。ゆっくり休んでいこう。");
            if (OnUpgradeRequested != null)
            {
                await OnUpgradeRequested(player, this);
            }
        }
    }

    public bool TryBuy(Player player, Summon summon)
    {
        if (player.SealTurns > 0) return false;

        // 念のためのチェック（基本はUI側で弾きますが、Core側でも担保します）
        if (summon == null) return false;

        // マスの基本価格（BaseValue）を支払えるかチェック
        if (player.Crystal >= BaseValue)
        {
            player.AddCrystal(-BaseValue);
            Owner = player;
            PlacedSummon = summon; // 購入と同時に必ず召喚獣をセット！
            return true;
        }
        return false;
    }
    
    public bool TryUpgrade(Player player)
    {
        if (player.SealTurns > 0) return false;
        
        // レベルアップ費用を計算（例：基本価格 × 現在のレベル）
        int upgradeCost = BaseValue * Level;

        if (player.Crystal >= upgradeCost)
        {
            player.AddCrystal(-upgradeCost);
            Level++;

            // 以前作成した「召喚獣の特殊能力トリガー」をここで呼び出せます！
            // （例：カーバンクルが配置されていれば、ここでキャッシュバック能力が発動する）
            PlacedSummon?.OnSquareUpgraded(this, player);
            
            return true;
        }
        return false;
    }
}