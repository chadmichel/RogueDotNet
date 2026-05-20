namespace Rogue.Entities;

public static class MonsterFactory
{
    public static Monster Create(int x, int y, int depth, Random rng)
    {
        var pool = new List<(int weight, Func<Monster> make)>
        {
            (6, () => CreateRat())
        };
        if (depth >= 2) pool.Add((5, () => new Monster { Kind = MonsterKind.Goblin, Name = "goblin", Glyph = "g", Color = ConsoleColor.Green,     Hp = 8,  MaxHp = 8,  Attack = 3 }));
        if (depth >= 3) pool.Add((4, () => new Monster { Kind = MonsterKind.Orc,    Name = "orc",    Glyph = "o", Color = ConsoleColor.DarkGreen, Hp = 14, MaxHp = 14, Attack = 5 }));
        if (depth >= 5) pool.Add((3, () => new Monster { Kind = MonsterKind.Troll,  Name = "troll",  Glyph = "T", Color = ConsoleColor.Magenta,   Hp = 24, MaxHp = 24, Attack = 7 }));
        if (depth >= 7) pool.Add((2, () => new Monster { Kind = MonsterKind.Wraith, Name = "wraith", Glyph = "W", Color = ConsoleColor.Cyan,      Hp = 32, MaxHp = 32, Attack = 10 }));

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

    public static Monster CreateRat(int x, int y)
    {
        var m = CreateRat();
        m.X = x;
        m.Y = y;
        return m;
    }

    public static Monster CreatePrimate(int x, int y)
    {
        return new Monster
        {
            Kind = MonsterKind.Primate,
            Name = "evil primate",
            Glyph = "😈",
            Color = ConsoleColor.Magenta,
            Hp = 3,
            MaxHp = 3,
            Attack = 5,
            X = x,
            Y = y,
            IsStationary = true,
            UsesHitCounter = true,
            HitsRemaining = 3
        };
    }

    private static Monster CreateRat()
    {
        return new Monster
        {
            Kind = MonsterKind.Rat,
            Name = "rat",
            Glyph = "🐀",
            Color = ConsoleColor.DarkGray,
            Hp = 4,
            MaxHp = 4,
            Attack = 2
        };
    }
}
