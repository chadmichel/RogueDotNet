using Rogue.Entities;
using Rogue.Items;
using Rogue.Map;
using Rogue.UI;

namespace Rogue;

public class Game
{
    private const int FovRadius = 8;
    private const string HighScoreFile = "highscores.txt";
    private const int RatKillsRequiredForPrimate = 2;

    private readonly Random _rng = new();
    private readonly Renderer _renderer = new();
    private readonly MessageLog _log = new();
    private readonly Player _player = new();
    private DungeonLevel _level = null!;
    private int _ratKillsForPrimate;
    private bool _primateSpawned;
    private bool _primateDefeated;
    private bool _primateScoreRecorded;
    private bool _quit;

    private readonly record struct ScoreEntry(int Score, int Depth, int Kills, DateTime Date);

    public void Run()
    {
        Console.CursorVisible = false;
        Console.Clear();
        ShowRules();
        GenerateLevel();
        _log.Add($"Welcome to the dungeon. Depth {_player.Depth}.", ConsoleColor.Yellow);
        _log.Add("Find the stairs. The first gate opens after the evil primate falls.", ConsoleColor.Cyan);

        while (!_quit && _player.IsAlive)
        {
            Fov.Compute(_level, _player.X, _player.Y, FovRadius);
            _renderer.Render(_level, _player, _log);

            var key = Console.ReadKey(true);
            bool tookTurn = HandleInput(key);
            if (tookTurn && _player.IsAlive)
                MonstersAct();
        }

        if (!_player.IsAlive)
        {
            Fov.Compute(_level, _player.X, _player.Y, FovRadius);
            _renderer.Render(_level, _player, _log);
            ShowGameOver();
        }
        else
        {
            Console.Clear();
            Console.ResetColor();
            Console.CursorVisible = true;
        }
    }

    private void GenerateLevel()
    {
        var gen = new MapGenerator(_rng);
        _level = gen.Generate(Renderer.MapWidth, Renderer.MapHeight, _player.Depth);
        _player.X = _level.PlayerSpawn.X;
        _player.Y = _level.PlayerSpawn.Y;
        _level.StairsLocked = _player.Depth == 1 && !_primateDefeated;
        if (_level.StairsLocked)
            EnsureRatGateCanOpen();
    }

    private bool HandleInput(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.Enter:      return TryPickUp();
            case ConsoleKey.UpArrow:    return TryMove(0, -1);
            case ConsoleKey.DownArrow:  return TryMove(0, 1);
            case ConsoleKey.LeftArrow:  return TryMove(-1, 0);
            case ConsoleKey.RightArrow: return TryMove(1, 0);
        }
        switch (key.KeyChar)
        {
            case 'f': case 'F': return TryFireGun();
            case 'i': case 'I': ShowInventory(); return false;
            case '>':           return TryDescend();
            case 'q': case 'Q': _quit = true; return false;
        }
        return false;
    }

    private static void ShowRules()
    {
        string[] rules =
        {
            "You are the running rogue: 🏃.",
            "Use the arrow keys to move through rooms and narrow corridors.",
            "Bump ordinary monsters to attack them. Rats look like 🐀.",
            "Stand on an item and press Enter to collect it. Stronger weapons equip automatically.",
            "Green + energy restores 50% of your maximum energy as soon as you collect it.",
            "Press i to open your inventory, then press a letter to use or equip an item.",
            "The first stairs are sealed. Kill 2 rats to draw out the evil primate.",
            "The evil primate is 😈 and blocks the way forward.",
            "When the primate appears, find the nearby 🔫 fire gun and press Enter to collect it.",
            "Stand next to 😈 and press f to fire. It takes exactly 3 fire shots to eliminate it.",
            "After 😈 is eliminated, the stairs unlock. Stand on > and press > to descend.",
            "Your score is based on depth and kills. Primate victory and game over record scores.",
            "Press q during play to quit."
        };

        for (int shown = 1; shown <= rules.Length; shown++)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("ROGUE RULES");
            Console.WriteLine("===========");
            Console.ResetColor();
            Console.WriteLine();

            for (int i = 0; i < shown; i++)
            {
                Console.ForegroundColor = i == shown - 1 ? ConsoleColor.White : ConsoleColor.Gray;
                Console.WriteLine($"{i + 1,2}. {rules[i]}");
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(shown < rules.Length
                ? "Press Enter to read the next rule."
                : "Press Enter to launch the game.");
            Console.ResetColor();

            WaitForEnter();
        }
        Console.Clear();
    }

    private static void WaitForEnter()
    {
        while (Console.ReadKey(true).Key != ConsoleKey.Enter)
        {
        }
    }

    private bool TryFireGun()
    {
        var primate = AdjacentPrimate();
        if (primate == null)
        {
            _log.Add("No primate is close enough to shoot.", ConsoleColor.Yellow);
            return false;
        }

        var equipped = _player.Inventory.EquippedWeapon;
        if (equipped == null || !equipped.IsFireWeapon)
        {
            var gun = _player.Inventory.Items.FirstOrDefault(i => i.IsFireWeapon);
            if (gun != null)
            {
                _player.Inventory.EquippedWeapon = gun;
                _log.Add($"You ready the {gun.Name}: {gun.FireCharges}/{gun.MaxFireCharges} fire shots.", ConsoleColor.Cyan);
            }
        }

        AttackMonster(primate);
        return true;
    }

    private Monster? AdjacentPrimate()
    {
        foreach (var n in Neighbors(_player.X, _player.Y))
        {
            var monster = _level.MonsterAt(n.X, n.Y);
            if (monster?.Kind == MonsterKind.Primate)
                return monster;
        }

        return null;
    }

    private bool TryMove(int dx, int dy)
    {
        int nx = _player.X + dx;
        int ny = _player.Y + dy;
        var monster = _level.MonsterAt(nx, ny);
        if (monster != null)
        {
            AttackMonster(monster);
            return true;
        }
        if (!_level.IsWalkable(nx, ny)) return false;

        _player.X = nx;
        _player.Y = ny;
        var here = _level.ItemAt(nx, ny);
        if (here != null)
            _log.Add($"You see a {here.Item.Name} here. (press Enter)", ConsoleColor.Cyan);
        if (_level.Tiles[nx, ny].Type == TileType.StairsDown)
            _log.Add(_level.StairsLocked
                ? "The stairs are sealed until the primate is slain."
                : "Stairs lead down here. (press >)",
                ConsoleColor.Yellow);
        return true;
    }

    private void AttackMonster(Monster m)
    {
        if (m.Kind == MonsterKind.Primate)
        {
            AttackPrimate(m);
        }
        else if (m.UsesHitCounter)
        {
            m.HitsRemaining--;
            m.Hp = Math.Max(0, m.HitsRemaining);
            if (m.HitsRemaining > 0)
            {
                string hitText = m.HitsRemaining == 1 ? "hit" : "hits";
                _log.Add($"You strike the {m.Name}. {m.HitsRemaining} {hitText} left.", ConsoleColor.White);
            }
            else
            {
                _log.Add($"You land the final hit on the {m.Name}.", ConsoleColor.White);
            }
        }
        else
        {
            int dmg = Math.Max(1, _player.Attack + _rng.Next(-1, 2));
            m.Hp -= dmg;
            _log.Add($"You hit the {m.Name} for {dmg}.", ConsoleColor.White);
        }

        if (!m.IsAlive)
        {
            _log.Add($"You kill the {m.Name}!", ConsoleColor.Green);
            _player.Kills++;
            _level.Monsters.Remove(m);
            HandleMonsterKilled(m);
        }
    }

    private void AttackPrimate(Monster m)
    {
        var weapon = _player.Inventory.EquippedWeapon;
        if (weapon == null || !weapon.IsFireWeapon)
        {
            _log.Add("The primate blocks your strike. Only fire can hurt it.", ConsoleColor.DarkYellow);
            return;
        }

        if (weapon.FireCharges <= 0)
        {
            _log.Add($"The {weapon.Name} is out of fire.", ConsoleColor.Red);
            return;
        }

        weapon.FireCharges--;
        m.HitsRemaining--;
        m.Hp = Math.Max(0, m.HitsRemaining);

        if (m.HitsRemaining > 0)
        {
            string hitText = m.HitsRemaining == 1 ? "blast" : "blasts";
            _log.Add($"The {weapon.Name} shoots fire! {m.HitsRemaining} {hitText} left.", ConsoleColor.Yellow);
        }
        else
        {
            _log.Add($"The {weapon.Name} fires its final blast!", ConsoleColor.Yellow);
        }
    }

    private void HandleMonsterKilled(Monster m)
    {
        if (m.Kind == MonsterKind.Rat && _level.StairsLocked && !_primateSpawned)
        {
            _ratKillsForPrimate++;
            int remaining = RatKillsRequiredForPrimate - _ratKillsForPrimate;
            if (remaining > 0)
            {
                _log.Add($"The sealed stairs stir. Kill {remaining} more rat.", ConsoleColor.DarkYellow);
            }
            else
            {
                SpawnPrimateGatekeeper();
            }
        }

        if (m.Kind == MonsterKind.Primate)
            UnlockPrimateGate();
    }

    private void SpawnPrimateGatekeeper()
    {
        var gate = FindPrimateGatePosition();
        if (gate == null)
        {
            _level.StairsLocked = false;
            _primateDefeated = true;
            _log.Add("The sealed stairs open.", ConsoleColor.Yellow);
            return;
        }

        _primateSpawned = true;
        _level.Monsters.Add(MonsterFactory.CreatePrimate(gate.Value.X, gate.Value.Y));
        DropFireGun();
        _log.Add("An evil primate blocks the narrow way to the stairs!", ConsoleColor.Red);
    }

    private void DropFireGun()
    {
        var weapon = _player.Inventory.Items.FirstOrDefault(i => i.IsFireWeapon);
        if (weapon != null)
        {
            _player.Inventory.EquippedWeapon = weapon;
            _log.Add($"You ready the {weapon.Name}: {weapon.FireCharges} fire shots.", ConsoleColor.Yellow);
            return;
        }

        if (_level.Items.Any(i => i.Item.IsFireWeapon))
            return;

        var gun = ItemFactory.CreateFireGun();
        var spot = FindFireGunDropPosition();
        if (spot == null)
        {
            if (!_player.Inventory.Add(gun))
                _player.Inventory.Items.Add(gun);
            _player.Inventory.EquippedWeapon = gun;
            _log.Add($"A {gun.Name} appears in your hand: {gun.FireCharges} fire shots.", ConsoleColor.Yellow);
            return;
        }

        _level.Items.Add(new ItemEntity { X = spot.Value.X, Y = spot.Value.Y, Item = gun });
        _log.Add($"A {gun.Name} appears nearby. Stand on 🔫 and press Enter.", ConsoleColor.Yellow);
    }

    private (int X, int Y)? FindFireGunDropPosition()
    {
        foreach (var n in Neighbors(_player.X, _player.Y))
            if (IsOpenForItem(n.X, n.Y))
                return n;

        var fallback = OpenFloorPositions()
            .Where(p => _level.ItemAt(p.X, p.Y) == null)
            .ToList();
        return fallback.Count == 0 ? null : fallback[_rng.Next(fallback.Count)];
    }

    private bool IsOpenForItem(int x, int y)
    {
        return _level.IsWalkable(x, y)
            && _level.Tiles[x, y].Type == TileType.Floor
            && (x != _player.X || y != _player.Y)
            && _level.MonsterAt(x, y) == null
            && _level.ItemAt(x, y) == null;
    }

    private (int X, int Y)? FindPrimateGatePosition()
    {
        var path = FindPath((_player.X, _player.Y), _level.StairsDown);
        if (path.Count > 0)
        {
            foreach (var step in path.AsEnumerable().Reverse())
            {
                if (IsOpenForPrimate(step.X, step.Y) && IsNarrowPath(step.X, step.Y))
                    return step;
            }

            foreach (var step in path.AsEnumerable().Reverse())
            {
                if (IsOpenForPrimate(step.X, step.Y))
                    return step;
            }
        }

        var fallback = OpenFloorPositions().ToList();
        return fallback.Count == 0 ? null : fallback[_rng.Next(fallback.Count)];
    }

    private List<(int X, int Y)> FindPath((int X, int Y) start, (int X, int Y) goal)
    {
        var visited = new bool[_level.Width, _level.Height];
        var cameFrom = new (int X, int Y)?[_level.Width, _level.Height];
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue(start);
        visited[start.X, start.Y] = true;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == goal)
                break;

            foreach (var next in Neighbors(current.X, current.Y))
            {
                if (visited[next.X, next.Y]) continue;
                if (!_level.IsWalkable(next.X, next.Y)) continue;

                visited[next.X, next.Y] = true;
                cameFrom[next.X, next.Y] = current;
                queue.Enqueue(next);
            }
        }

        if (!visited[goal.X, goal.Y])
            return new List<(int X, int Y)>();

        var path = new List<(int X, int Y)>();
        var step = goal;
        while (step != start)
        {
            path.Add(step);
            step = cameFrom[step.X, step.Y]!.Value;
        }
        path.Add(start);
        path.Reverse();
        return path;
    }

    private bool IsOpenForPrimate(int x, int y)
    {
        return _level.IsWalkable(x, y)
            && _level.Tiles[x, y].Type != TileType.StairsDown
            && (x != _player.X || y != _player.Y)
            && _level.MonsterAt(x, y) == null;
    }

    private bool IsNarrowPath(int x, int y)
    {
        int openNeighbors = 0;
        foreach (var n in Neighbors(x, y))
            if (_level.IsWalkable(n.X, n.Y))
                openNeighbors++;
        return openNeighbors <= 2;
    }

    private void EnsureRatGateCanOpen()
    {
        int ratCount = _level.Monsters.Count(m => m.Kind == MonsterKind.Rat);
        while (ratCount < RatKillsRequiredForPrimate)
        {
            var spot = OpenFloorPositions().ToList();
            if (spot.Count == 0)
                return;

            var (x, y) = spot[_rng.Next(spot.Count)];
            _level.Monsters.Add(MonsterFactory.CreateRat(x, y));
            ratCount++;
        }
    }

    private IEnumerable<(int X, int Y)> OpenFloorPositions()
    {
        for (int x = 0; x < _level.Width; x++)
            for (int y = 0; y < _level.Height; y++)
                if (_level.Tiles[x, y].Type == TileType.Floor
                    && (x != _player.X || y != _player.Y)
                    && _level.MonsterAt(x, y) == null)
                    yield return (x, y);
    }

    private static IEnumerable<(int X, int Y)> Neighbors(int x, int y)
    {
        yield return (x + 1, y);
        yield return (x - 1, y);
        yield return (x, y + 1);
        yield return (x, y - 1);
    }

    private bool TryPickUp()
    {
        var ie = _level.ItemAt(_player.X, _player.Y);
        if (ie == null)
        {
            _log.Add("Nothing to pick up.", ConsoleColor.DarkGray);
            return false;
        }
        if (ie.Item.Kind == ItemKind.EnergyPlus)
            return CollectEnergyPlus(ie);

        if (!_player.Inventory.Add(ie.Item))
        {
            _log.Add("Your pack is full.", ConsoleColor.Red);
            return false;
        }
        _level.Items.Remove(ie);
        _log.Add($"You pick up the {ie.Item.Name}.", ConsoleColor.Cyan);
        AutoEquipPickedWeapon(ie.Item);
        return true;
    }

    private bool CollectEnergyPlus(ItemEntity ie)
    {
        int healed = HealPlayerByPercent(ie.Item.HealPercent);
        _level.Items.Remove(ie);
        _log.Add(healed > 0
            ? $"Green energy restores {healed} energy."
            : "Green energy hums, but your energy is already full.",
            healed > 0 ? ConsoleColor.Green : ConsoleColor.DarkGray);
        return true;
    }

    private int HealPlayerByPercent(int percent)
    {
        int amount = Math.Max(1, _player.MaxHp * percent / 100);
        int healed = Math.Min(amount, _player.MaxHp - _player.Hp);
        _player.Hp += healed;
        return healed;
    }

    private void AutoEquipPickedWeapon(Item item)
    {
        if (item.Kind != ItemKind.Weapon)
            return;

        var equipped = _player.Inventory.EquippedWeapon;
        bool shouldEquip = item.IsFireWeapon
            || equipped == null
            || (!equipped.IsFireWeapon && item.AttackBonus >= equipped.AttackBonus);

        if (!shouldEquip)
        {
            _log.Add($"You keep the {item.Name} in your pack.", ConsoleColor.DarkGray);
            return;
        }

        _player.Inventory.EquippedWeapon = item;
        _log.Add(item.IsFireWeapon
            ? $"You ready the {item.Name}. {item.FireCharges} fire shots loaded."
            : $"You equip the {item.Name}. Power rises to {_player.Attack} ATK.",
            ConsoleColor.Cyan);
    }

    private void ShowInventory()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("-- INVENTORY --");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("Press a letter to use/equip, or any other key to cancel.");
        Console.WriteLine();
        if (_player.Inventory.Items.Count == 0)
        {
            Console.WriteLine("  (empty)");
        }
        else
        {
            for (int i = 0; i < _player.Inventory.Items.Count; i++)
            {
                char letter = (char)('a' + i);
                var item = _player.Inventory.Items[i];
                string detail = item.Kind switch
                {
                    ItemKind.HealingPotion => $"heals {item.HealAmount}",
                    ItemKind.EnergyPlus => $"restores {item.HealPercent}% energy",
                    ItemKind.Weapon when item.IsFireWeapon => $"{item.FireCharges}/{item.MaxFireCharges} fire shots",
                    ItemKind.Weapon => $"+{item.AttackBonus} atk",
                    _ => ""
                };
                string equipped = item == _player.Inventory.EquippedWeapon ? " (equipped)" : "";
                Console.ForegroundColor = item.Color;
                Console.Write($"  {letter}) ");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"{item.Name} [{detail}]{equipped}");
            }
        }
        Console.ResetColor();
        var key = Console.ReadKey(true);
        int idx = key.KeyChar - 'a';
        if (idx >= 0 && idx < _player.Inventory.Items.Count)
            UseItem(_player.Inventory.Items[idx]);
        _renderer.Reset();
    }

    private void UseItem(Item item)
    {
        switch (item.Kind)
        {
            case ItemKind.HealingPotion:
                int healed = Math.Min(item.HealAmount, _player.MaxHp - _player.Hp);
                _player.Hp += healed;
                _log.Add(healed > 0
                    ? $"You drink the {item.Name}. (+{healed} HP)"
                    : $"You drink the {item.Name}. No effect.",
                    ConsoleColor.Green);
                _player.Inventory.Remove(item);
                break;
            case ItemKind.EnergyPlus:
                int energy = HealPlayerByPercent(item.HealPercent);
                _log.Add(energy > 0
                    ? $"Green energy restores {energy} energy."
                    : "Your energy is already full.",
                    energy > 0 ? ConsoleColor.Green : ConsoleColor.DarkGray);
                _player.Inventory.Remove(item);
                break;
            case ItemKind.Weapon:
                _player.Inventory.EquippedWeapon = item;
                _log.Add(item.IsFireWeapon
                    ? $"You equip the {item.Name}. {item.FireCharges} fire shots ready."
                    : $"You equip the {item.Name}.",
                    ConsoleColor.Cyan);
                break;
        }
    }

    private bool TryDescend()
    {
        if (_level.Tiles[_player.X, _player.Y].Type != TileType.StairsDown)
        {
            _log.Add("No stairs here.", ConsoleColor.DarkGray);
            return false;
        }
        if (_level.StairsLocked)
        {
            _log.Add(_primateSpawned
                ? "The primate still blocks the way to the next level."
                : "The stairs are sealed. Kill two rats to draw out the primate.",
                ConsoleColor.DarkYellow);
            return false;
        }
        _player.Depth++;
        _player.Hp = _player.MaxHp;
        GenerateLevel();
        _log.Add($"You descend to depth {_player.Depth}. Energy restored to full.", ConsoleColor.Yellow);
        return true;
    }

    private void MonstersAct()
    {
        foreach (var m in _level.Monsters.ToList())
        {
            if (!m.IsAlive) continue;
            if (!_level.Tiles[m.X, m.Y].Visible) continue;

            int dx = _player.X - m.X;
            int dy = _player.Y - m.Y;

            if (Math.Abs(dx) + Math.Abs(dy) == 1)
            {
                int dmg = Math.Max(1, m.Attack + _rng.Next(-1, 2));
                _player.Hp -= dmg;
                _log.Add($"The {m.Name} hits you for {dmg}.", ConsoleColor.Red);
                if (!_player.IsAlive)
                {
                    _log.Add($"The {m.Name} kills you...", ConsoleColor.Red);
                    return;
                }
                continue;
            }

            if (m.IsStationary)
                continue;

            int sx = Math.Sign(dx);
            int sy = Math.Sign(dy);
            bool preferX = Math.Abs(dx) >= Math.Abs(dy);
            if (preferX)
            {
                if (sx != 0 && TryStepMonster(m, sx, 0)) continue;
                if (sy != 0) TryStepMonster(m, 0, sy);
            }
            else
            {
                if (sy != 0 && TryStepMonster(m, 0, sy)) continue;
                if (sx != 0) TryStepMonster(m, sx, 0);
            }
        }
    }

    private bool TryStepMonster(Monster m, int dx, int dy)
    {
        int nx = m.X + dx;
        int ny = m.Y + dy;
        if (!_level.IsWalkable(nx, ny)) return false;
        if (_level.MonsterAt(nx, ny) != null) return false;
        if (nx == _player.X && ny == _player.Y) return false;
        m.X = nx;
        m.Y = ny;
        return true;
    }

    private void UnlockPrimateGate()
    {
        _level.StairsLocked = false;
        _primateDefeated = true;
        _log.Add("The way opens. A new level is unlocked.", ConsoleColor.Yellow);

        if (_primateScoreRecorded)
            return;

        RecordCurrentScore();
        _primateScoreRecorded = true;
        _log.Add($"Score recorded: {_player.Score}.", ConsoleColor.Cyan);
    }

    private void ShowGameOver()
    {
        Console.ReadKey(true);
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine();
        Console.WriteLine("    ##############################");
        Console.WriteLine("    #         GAME OVER          #");
        Console.WriteLine("    ##############################");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine();
        Console.WriteLine($"    You reached depth {_player.Depth}.");
        Console.WriteLine($"    Slain monsters: {_player.Kills}");
        Console.WriteLine($"    Final score:    {_player.Score}");
        Console.WriteLine();

        var (entry, scores) = RecordCurrentScore();

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("    -- HIGH SCORES --");
        int rank = 1;
        foreach (var s in scores)
        {
            bool isMine = s == entry;
            Console.ForegroundColor = isMine ? ConsoleColor.Cyan : ConsoleColor.Gray;
            string marker = isMine ? " *" : "  ";
            Console.WriteLine($"   {rank,2}.{marker}{s.Score,5}   depth {s.Depth,2}   kills {s.Kills,3}");
            rank++;
        }
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("    Press any key to exit.");
        Console.ReadKey(true);
        Console.ResetColor();
        Console.CursorVisible = true;
    }

    private (ScoreEntry Entry, List<ScoreEntry> Scores) RecordCurrentScore()
    {
        var scores = LoadScores();
        var entry = new ScoreEntry(_player.Score, _player.Depth, _player.Kills, DateTime.Now);
        scores.Add(entry);
        scores = scores.OrderByDescending(s => s.Score).Take(10).ToList();
        SaveScores(scores);
        return (entry, scores);
    }

    private static List<ScoreEntry> LoadScores()
    {
        var list = new List<ScoreEntry>();
        if (!File.Exists(HighScoreFile)) return list;
        foreach (var line in File.ReadAllLines(HighScoreFile))
        {
            var parts = line.Split('|');
            if (parts.Length != 4) continue;
            if (int.TryParse(parts[0], out int s)
                && int.TryParse(parts[1], out int d)
                && int.TryParse(parts[2], out int k)
                && DateTime.TryParse(parts[3], out DateTime dt))
                list.Add(new ScoreEntry(s, d, k, dt));
        }
        return list;
    }

    private static void SaveScores(List<ScoreEntry> scores)
    {
        var lines = scores.Select(s => $"{s.Score}|{s.Depth}|{s.Kills}|{s.Date:O}");
        File.WriteAllLines(HighScoreFile, lines);
    }
}
