using Rogue.Entities;
using Rogue.Items;
using Rogue.Map;
using Rogue.UI;

namespace Rogue;

public class Game
{
    private const int FovRadius = 8;
    private const string HighScoreFile = "highscores.txt";

    private readonly Random _rng = new();
    private readonly Renderer _renderer = new();
    private readonly MessageLog _log = new();
    private readonly Player _player = new();
    private readonly Dictionary<int, DungeonLevel> _levels = new();
    private DungeonLevel _level = null!;
    private bool _quit;
    private bool _bossSpawned;

    public void Run()
    {
        Console.CursorVisible = false;
        Console.Clear();
        EnterLevel(1, fromAbove: true);
        _log.Add($"Welcome to the dungeon. Depth {_player.Depth}.", ConsoleColor.Yellow);

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

    private void EnterLevel(int depth, bool fromAbove)
    {
        if (!_levels.TryGetValue(depth, out var level))
        {
            var gen = new MapGenerator(_rng);
            level = gen.Generate(Renderer.MapWidth, Renderer.MapHeight, depth, !_bossSpawned);
            _levels[depth] = level;
            if (level.Monsters.Any(m => m.IsBoss))
            {
                _bossSpawned = true;
                _log.Add("An ominous presence stalks this floor...", ConsoleColor.DarkRed);
            }
        }
        _level = level;
        _player.Depth = depth;

        if (depth > _player.MaxDepth)
        {
            _player.MaxDepth = depth;
            if (depth > 1)
            {
                int heal = Math.Min(5, _player.MaxHp - _player.Hp);
                _player.Hp += heal;
            }
        }

        (int X, int Y) spawn;
        if (!fromAbove)
            spawn = _level.StairsDown;
        else if (_level.HasStairsUp)
            spawn = _level.StairsUp;
        else
            spawn = _level.PlayerSpawn;

        _player.X = spawn.X;
        _player.Y = spawn.Y;
        _player.UpdateAppearance();
    }

    private bool HandleInput(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.UpArrow: return TryMove(0, -1);
            case ConsoleKey.DownArrow: return TryMove(0, 1);
            case ConsoleKey.LeftArrow: return TryMove(-1, 0);
            case ConsoleKey.RightArrow: return TryMove(1, 0);
        }
        switch (key.KeyChar)
        {
            case 'g': case 'G': return TryPickUp();
            case 'i': case 'I': ShowInventory(); return false;
            case '>': return TryDescend();
            case '<': return TryAscend();
            case 'z': case 'Z': return TryZap();
            case 'q': case 'Q': _quit = true; return false;
        }
        return false;
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

        TryUseFountain(nx, ny);

        var here = _level.ItemAt(nx, ny);
        if (here != null)
            _log.Add($"You see a {here.Item.DisplayName} here. (press g)", ConsoleColor.Cyan);
        if (_level.Tiles[nx, ny].Type == TileType.StairsDown)
            _log.Add("Stairs lead down here. (press >)", ConsoleColor.Yellow);
        else if (_level.Tiles[nx, ny].Type == TileType.StairsUp)
            _log.Add("Stairs lead up here. (press <)", ConsoleColor.Yellow);
        return true;
    }

    private void TryUseFountain(int x, int y)
    {
        if (_level.Tiles[x, y].Type != TileType.Fountain) return;

        var weapon = _player.Inventory.EquippedWeapon;
        if (weapon == null)
        {
            _log.Add("A fountain hums with magic, but you have no weapon equipped.", ConsoleColor.Yellow);
            return;
        }
        if (weapon.IsEnchanted)
        {
            _log.Add($"The fountain's power has already blessed your {weapon.DisplayName}.", ConsoleColor.DarkGray);
            return;
        }

        weapon.IsEnchanted = true;
        _player.UpdateAppearance();
        _log.Add($"Your {weapon.DisplayName} glows with enchantment!", ConsoleColor.Cyan);
    }

    private void AttackMonster(Monster m)
    {
        if (m.IsBoss && _player.Inventory.EquippedWeapon?.IsEnchanted != true)
        {
            _log.Add($"Your attack glances off the {m.Name}. Only enchanted steel can harm it!", ConsoleColor.Yellow);
            return;
        }

        int dmg = Math.Max(1, _player.Attack + _rng.Next(-1, 2));
        m.Hp -= dmg;
        _log.Add($"You hit the {m.Name} for {dmg}.", ConsoleColor.White);
        if (!m.IsAlive)
        {
            _log.Add($"You kill the {m.Name}!", ConsoleColor.Green);
            _player.Kills++;
            _level.Monsters.Remove(m);
        }
    }

    private bool TryPickUp()
    {
        var ie = _level.ItemAt(_player.X, _player.Y);
        if (ie == null)
        {
            _log.Add("Nothing to pick up.", ConsoleColor.DarkGray);
            return false;
        }
        if (!_player.Inventory.Add(ie.Item))
        {
            _log.Add("Your pack is full.", ConsoleColor.Red);
            return false;
        }
        _level.Items.Remove(ie);
        _log.Add($"You pick up the {ie.Item.DisplayName}.", ConsoleColor.Cyan);
        return true;
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
                    ItemKind.Weapon => $"+{item.AttackBonus} atk",
                    ItemKind.Wand => $"{item.WandDamage} dmg, range {item.WandRange}",
                    _ => ""
                };
                string equipped = item == _player.Inventory.EquippedWeapon || item == _player.Inventory.EquippedWand ? " (equipped)" : "";
                Console.ForegroundColor = item.Color;
                Console.Write($"  {letter}) ");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"{item.DisplayName} [{detail}]{equipped}");
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
                    ? $"You drink the {item.DisplayName}. (+{healed} HP)"
                    : $"You drink the {item.DisplayName}. No effect.",
                    ConsoleColor.Green);
                _player.Inventory.Remove(item);
                _player.UpdateAppearance();
                break;
            case ItemKind.Weapon:
                _player.Inventory.EquippedWeapon = item;
                _player.UpdateAppearance();
                _log.Add($"You equip the {item.DisplayName}.", ConsoleColor.Cyan);
                break;
            case ItemKind.Wand:
                _player.Inventory.EquippedWand = item;
                _log.Add($"You ready the {item.DisplayName}.", ConsoleColor.Magenta);
                break;
        }
    }

    private bool TryDescend()
    {
        if (_level.Tiles[_player.X, _player.Y].Type != TileType.StairsDown)
        {
            _log.Add("No stairs down here.", ConsoleColor.DarkGray);
            return false;
        }
        EnterLevel(_player.Depth + 1, fromAbove: true);
        _log.Add($"You descend to depth {_player.Depth}.", ConsoleColor.Yellow);
        return true;
    }

    private bool TryZap()
    {
        var wand = _player.Inventory.EquippedWand;
        if (wand == null)
        {
            _log.Add("You have no wand readied.", ConsoleColor.DarkGray);
            return false;
        }
        if (wand.Charges <= 0)
        {
            _log.Add($"The {wand.Name} is spent.", ConsoleColor.DarkGray);
            return false;
        }

        _log.Add("Zap which direction? (arrow keys)", ConsoleColor.Magenta);
        Fov.Compute(_level, _player.X, _player.Y, FovRadius);
        _renderer.Render(_level, _player, _log);

        var dirKey = Console.ReadKey(true);
        int dx = 0, dy = 0;
        switch (dirKey.Key)
        {
            case ConsoleKey.UpArrow:    dy = -1; break;
            case ConsoleKey.DownArrow:  dy =  1; break;
            case ConsoleKey.LeftArrow:  dx = -1; break;
            case ConsoleKey.RightArrow: dx =  1; break;
            default:
                _log.Add("You hold the wand. Nothing happens.", ConsoleColor.DarkGray);
                return false;
        }

        FireFireball(wand, dx, dy);
        wand.Charges--;
        return true;
    }

    private void FireFireball(Item wand, int dx, int dy)
    {
        int x = _player.X;
        int y = _player.Y;
        for (int step = 0; step < wand.WandRange; step++)
        {
            x += dx;
            y += dy;
            if (!_level.InBounds(x, y))
            {
                _log.Add("The fireball flies off into the dark.", ConsoleColor.DarkGray);
                return;
            }
            if (!_level.Tiles[x, y].IsWalkable)
            {
                _renderer.DrawTransient(x, y, "🔥", ConsoleColor.Red);
                Thread.Sleep(60);
                _log.Add("The fireball bursts against the wall.", ConsoleColor.Red);
                return;
            }

            _renderer.DrawTransient(x, y, "🔥", ConsoleColor.Red);
            Thread.Sleep(40);

            var m = _level.MonsterAt(x, y);
            if (m != null)
            {
                int dmg = wand.WandDamage + _rng.Next(-1, 2);
                m.Hp -= dmg;
                _log.Add($"The fireball hits the {m.Name} for {dmg}!", ConsoleColor.Red);
                if (!m.IsAlive)
                {
                    _log.Add($"You burn the {m.Name} to ash!", ConsoleColor.Green);
                    _player.Kills++;
                    _level.Monsters.Remove(m);
                }
                return;
            }
        }
        _log.Add("The fireball fizzles in the air.", ConsoleColor.DarkGray);
    }

    private bool TryAscend()
    {
        if (_level.Tiles[_player.X, _player.Y].Type != TileType.StairsUp)
        {
            _log.Add("No stairs up here.", ConsoleColor.DarkGray);
            return false;
        }
        if (_player.Depth <= 1)
        {
            _log.Add("You cannot leave the dungeon yet.", ConsoleColor.Yellow);
            return false;
        }
        EnterLevel(_player.Depth - 1, fromAbove: false);
        _log.Add($"You ascend to depth {_player.Depth}.", ConsoleColor.Yellow);
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
        Console.WriteLine($"    You reached depth {_player.MaxDepth}.");
        Console.WriteLine($"    Slain monsters: {_player.Kills}");
        Console.WriteLine($"    Final score:    {_player.Score}");
        Console.WriteLine();

        var scores = LoadScores();
        var entry = (score: _player.Score, depth: _player.MaxDepth, kills: _player.Kills, date: DateTime.Now);
        scores.Add(entry);
        scores = scores.OrderByDescending(s => s.score).Take(10).ToList();
        SaveScores(scores);

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("    -- HIGH SCORES --");
        int rank = 1;
        foreach (var s in scores)
        {
            bool isMine = s.score == entry.score && s.date == entry.date;
            Console.ForegroundColor = isMine ? ConsoleColor.Cyan : ConsoleColor.Gray;
            string marker = isMine ? " *" : "  ";
            Console.WriteLine($"   {rank,2}.{marker}{s.score,5}   depth {s.depth,2}   kills {s.kills,3}");
            rank++;
        }
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("    Press any key to exit.");
        Console.ReadKey(true);
        Console.ResetColor();
        Console.CursorVisible = true;
    }

    private static List<(int score, int depth, int kills, DateTime date)> LoadScores()
    {
        var list = new List<(int, int, int, DateTime)>();
        if (!File.Exists(HighScoreFile)) return list;
        foreach (var line in File.ReadAllLines(HighScoreFile))
        {
            var parts = line.Split('|');
            if (parts.Length != 4) continue;
            if (int.TryParse(parts[0], out int s)
                && int.TryParse(parts[1], out int d)
                && int.TryParse(parts[2], out int k)
                && DateTime.TryParse(parts[3], out DateTime dt))
                list.Add((s, d, k, dt));
        }
        return list;
    }

    private static void SaveScores(List<(int score, int depth, int kills, DateTime date)> scores)
    {
        var lines = scores.Select(s => $"{s.score}|{s.depth}|{s.kills}|{s.date:O}");
        File.WriteAllLines(HighScoreFile, lines);
    }
}
