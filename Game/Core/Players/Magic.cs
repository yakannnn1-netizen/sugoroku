namespace Game.Core;

public abstract class Magic
{
    public string Name { get; }
    public int Cost { get; }


    public Magic(string name, int cost)
    {
        Name = name;
        Cost = cost;
    }

    public MagicCastResult Cast(Player caster, Player target)
    {
        // 1. まず相殺判定を行う（対象が同種の魔法を持っていれば消費して無効化）
        if (caster != target && target.Inventory.TryConsumeMagicByName(this.Name))
        {
            return new MagicCastResult(true, $"【相殺！】 {target.Name} はインベントリの『{Name}』を消費して無効化した！");
        }

        // 2. 次に状態異常の判定を行う（相殺できなかった場合で、すでに状態異常なら無効化）
        if (caster != target && target.IsStatusAffected)
        {
            return new MagicCastResult(false, $"【無効！】 {target.Name} はすでに状態異常にかかっているため、新たな魔法を受け付けない！");
        }

        // 3. 実際の効果を発動
        string effectMessage = ExecuteEffect(caster, target);
        return new MagicCastResult(false, effectMessage);
    }
    protected abstract string ExecuteEffect(Player caster, Player target);
}