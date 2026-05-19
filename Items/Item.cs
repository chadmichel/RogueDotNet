namespace Rogue.Items;

public enum ItemKind { HealingPotion, Weapon }

public class Item
{
    public string Name { get; set; } = "";
    public char Glyph { get; set; }
    public ConsoleColor Color { get; set; }
    public ItemKind Kind { get; set; }
    public int HealAmount { get; set; }
    public int AttackBonus { get; set; }
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
        if (rng.Next(100) < 65)
        {
            ie.Item = new Item
            {
                Name = "healing potion",
                Glyph = '!',
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
                Glyph = '/',
                Color = ConsoleColor.Cyan,
                Kind = ItemKind.Weapon,
                AttackBonus = bonus
            };
        }
        return ie;
    }
}
