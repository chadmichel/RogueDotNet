namespace Rogue.Items;

public enum ItemKind { HealingPotion, Weapon, EnergyPlus }

public class Item
{
    public string Name { get; set; } = "";
    public string Glyph { get; set; } = "";
    public ConsoleColor Color { get; set; }
    public ItemKind Kind { get; set; }
    public int HealAmount { get; set; }
    public int HealPercent { get; set; }
    public int AttackBonus { get; set; }
    public int FireCharges { get; set; }
    public int MaxFireCharges { get; set; }
    public bool IsFireWeapon => MaxFireCharges > 0;
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
    public const int FireGunCharges = 3;

    public static ItemEntity Create(int x, int y, int depth, Random rng)
    {
        var ie = new ItemEntity { X = x, Y = y };
        int roll = rng.Next(100);
        if (roll < 45)
        {
            ie.Item = new Item
            {
                Name = "healing potion",
                Glyph = "!",
                Color = ConsoleColor.Red,
                Kind = ItemKind.HealingPotion,
                HealAmount = 10 + rng.Next(6)
            };
        }
        else if (roll < 70)
        {
            ie.Item = CreateEnergyPlus();
        }
        else
        {
            int bonus = 1 + depth / 2 + rng.Next(2);
            int idx = Math.Clamp(bonus - 1, 0, WeaponNames.Length - 1);
            ie.Item = new Item
            {
                Name = WeaponNames[idx],
                Glyph = "/",
                Color = ConsoleColor.Cyan,
                Kind = ItemKind.Weapon,
                AttackBonus = bonus
            };
        }
        return ie;
    }

    public static Item CreateFireGun()
    {
        return new Item
        {
            Name = "fire gun",
            Glyph = "🔫",
            Color = ConsoleColor.Yellow,
            Kind = ItemKind.Weapon,
            FireCharges = FireGunCharges,
            MaxFireCharges = FireGunCharges
        };
    }

    public static Item CreateEnergyPlus()
    {
        return new Item
        {
            Name = "green energy",
            Glyph = "+",
            Color = ConsoleColor.Green,
            Kind = ItemKind.EnergyPlus,
            HealPercent = 50
        };
    }
}
