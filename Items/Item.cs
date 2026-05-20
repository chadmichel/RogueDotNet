namespace Rogue.Items;

public enum ItemKind { HealingPotion, Weapon, Wand }

public class Item
{
    public string Name { get; set; } = "";
    public string Glyph { get; set; } = "??";
    public ConsoleColor Color { get; set; }
    public ItemKind Kind { get; set; }
    public int HealAmount { get; set; }
    public int AttackBonus { get; set; }
    public bool IsEnchanted { get; set; }
    public int Charges { get; set; }
    public int WandDamage { get; set; }
    public int WandRange { get; set; }

    public string DisplayName => (Kind, IsEnchanted) switch
    {
        (ItemKind.Weapon, true) => $"enchanted {Name}",
        (ItemKind.Wand, _) => $"{Name} ({Charges})",
        _ => Name
    };
}

public class ItemEntity
{
    public int X { get; set; }
    public int Y { get; set; }
    public Item Item { get; set; } = new();
}

public static class ItemFactory
{
    private static readonly string[] WeaponNames = { "dagger", "shortsword", "longsword", "battle axe", "warhammer" };

    public static ItemEntity Create(int x, int y, int depth, Random rng)
    {
        var ie = new ItemEntity { X = x, Y = y };

        if (depth >= 2 && rng.Next(100) < 15)
        {
            ie.Item = new Item
            {
                Name = "wand of fireball",
                Glyph = "🪄",
                Color = ConsoleColor.Magenta,
                Kind = ItemKind.Wand,
                WandDamage = 8 + depth / 3,
                WandRange = 6,
                Charges = 3 + rng.Next(3)
            };
        }
        else if (rng.Next(100) < 55)
        {
            ie.Item = new Item
            {
                Name = "healing potion",
                Glyph = "🧪",
                Color = ConsoleColor.Red,
                Kind = ItemKind.HealingPotion,
                HealAmount = 10 + rng.Next(6)
            };
        }
        else
        {
            int bonus = 1 + depth / 2 + rng.Next(2);
            int idx = Math.Clamp(bonus - 1, 0, WeaponNames.Length - 1);
            ie.Item = new Item
            {
                Name = WeaponNames[idx],
                Glyph = "🗡️",
                Color = ConsoleColor.Cyan,
                Kind = ItemKind.Weapon,
                AttackBonus = bonus
            };
        }
        return ie;
    }
}
