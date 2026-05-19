using Rogue.Items;

namespace Rogue.Entities;

public class Player : Entity
{
    public int Hp { get; set; } = 30;
    public int MaxHp { get; set; } = 30;
    public int BaseAttack { get; set; } = 4;
    public int Depth { get; set; } = 1;
    public int Kills { get; set; }
    public Inventory Inventory { get; } = new();

    public int Attack => BaseAttack + (Inventory.EquippedWeapon?.AttackBonus ?? 0);
    public bool IsAlive => Hp > 0;
    public int Score => Depth * 100 + Kills * 10;

    public Player()
    {
        Glyph = '@';
        Color = ConsoleColor.White;
        Name = "you";
    }
}
