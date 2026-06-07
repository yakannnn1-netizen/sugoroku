namespace Game.Core;

public class SealMagic : Magic
{
    public SealMagic() : base("サイレス", 400) { }

    protected override string ExecuteEffect(Player caster, Player target)
    {
        target.SealTurns = 2; // 対象を2ターン封印状態（購入不可）にする
        return $"{target.Name} は封印された！（あと {target.SealTurns} ターン土地の購入・増資不可）";
    }
}