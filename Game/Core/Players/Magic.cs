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
        // ★追加: 自分以外の相手がすでに状態異常にかかっている場合は自動的に無効化する
        if (caster != target && target.IsStatusAffected)
        {
            return new MagicCastResult(false, $"【無効！】 {target.Name} はすでに状態異常にかかっているため、新たな魔法を受け付けない！");
        }

        // 相殺判定（防がれなかった状態異常中の場合は、ここに到達する前に自動で弾かれます）
        if (caster != target && target.Inventory.TryConsumeMagicByName(this.Name))
        {
            return new MagicCastResult(true, $"【相殺！】 {target.Name} はインベントリの『{Name}』を消費して無効化した！");
        }

        // 実際の効果を発動
        string effectMessage = ExecuteEffect(caster, target);
        return new MagicCastResult(false, effectMessage);
    }
    protected abstract string ExecuteEffect(Player caster, Player target);
}