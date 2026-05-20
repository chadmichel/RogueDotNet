namespace Rogue.Entities;

public enum MonsterKind { Rat, Goblin, Orc, Troll, Wraith, Primate }

public class Monster : Entity
{
    public MonsterKind Kind { get; set; }
    public int Hp { get; set; }
    public int MaxHp { get; set; }
    public int Attack { get; set; }
    public bool IsStationary { get; set; }
    public bool UsesHitCounter { get; set; }
    public int HitsRemaining { get; set; }
    public bool IsAlive => UsesHitCounter ? HitsRemaining > 0 : Hp > 0;
}
