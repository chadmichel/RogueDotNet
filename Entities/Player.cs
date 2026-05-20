using Rogue.Items;

namespace Rogue.Entities;

public class Player : Entity
{
    private const ConsoleColor DefaultColor = ConsoleColor.White;
    private const ConsoleColor EnchantedWeaponColor = ConsoleColor.Yellow;

    public int Hp { get; set; } = 30;
    public int MaxHp { get; set; } = 30;
    public int BaseAttack { get; set; } = 4;
    public int Depth { get; set; } = 1;
    public int MaxDepth { get; set; } = 1;
    public int Kills { get; set; }
    public Inventory Inventory { get; } = new();

    public int Attack => BaseAttack + (Inventory.EquippedWeapon?.AttackBonus ?? 0);
    public bool IsAlive => Hp > 0;
    public int Score => MaxDepth * 100 + Kills * 10;

    public Player()
    {
        Glyph = "🧙";
        Color = DefaultColor;
        Name = "you";
    }

    public void UpdateAppearance()
    {
        Color = Inventory.EquippedWeapon?.IsEnchanted == true
            ? EnchantedWeaponColor
            : DefaultColor;
    }
}
