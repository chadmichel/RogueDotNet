namespace Rogue.Entities;

public static class MonsterFactory
{
    public static Monster CreateBoss(int x, int y)
    {
        return new Monster
        {
            Name = "dread knight",
            Glyph = "💀",
            Color = ConsoleColor.DarkRed,
            Hp = 45,
            MaxHp = 45,
            Attack = 11,
            IsBoss = true,
            X = x,
            Y = y
        };
    }

    public static Monster Create(int x, int y, int depth, Random rng)
    {
        var pool = new List<(int weight, Func<Monster> make)>
        {
            (6, () => new Monster { Name = "rat",    Glyph = "🐀", Color = ConsoleColor.DarkGray,  Hp = 4,  MaxHp = 4,  Attack = 2 })
        };
        if (depth >= 2) pool.Add((5, () => new Monster { Name = "goblin", Glyph = "👺", Color = ConsoleColor.Green, Hp = 8, MaxHp = 8, Attack = 3 }));
        if (depth >= 3) pool.Add((4, () => new Monster { Name = "orc", Glyph = "👹", Color = ConsoleColor.DarkGreen, Hp = 14, MaxHp = 14, Attack = 5 }));
        if (depth >= 5) pool.Add((3, () => new Monster { Name = "troll", Glyph = "🧌", Color = ConsoleColor.Magenta, Hp = 24, MaxHp = 24, Attack = 7 }));
        if (depth >= 7) pool.Add((2, () => new Monster { Name = "wraith", Glyph = "👻", Color = ConsoleColor.Cyan, Hp = 32, MaxHp = 32, Attack = 10 }));

        int total = pool.Sum(p => p.weight);
        int roll = rng.Next(total);
        int acc = 0;
        Func<Monster> chosen = pool[0].make;
        foreach (var (w, make) in pool)
        {
            acc += w;
            if (roll < acc) { chosen = make; break; }
        }
        var m = chosen();
        m.X = x;
        m.Y = y;
        return m;
    }
}
