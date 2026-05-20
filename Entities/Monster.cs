namespace Rogue.Entities;

public class Monster : Entity
{
    public int Hp { get; set; }
    public int MaxHp { get; set; }
    public int Attack { get; set; }
    public bool IsBoss { get; set; }
    public bool IsAlive => Hp > 0;
}
