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
    private DungeonLevel _level = null!;
    private bool _quit;

    public void Run()
    {
        Console.CursorVisible = false;
        Console.Clear();
        GenerateLevel();
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

    private void GenerateLevel()
    {
        var gen = new MapGenerator(_rng);
        _level = gen.Generate(Renderer.MapWidth, Renderer.MapHeight, _player.Depth);
        _player.X = _level.PlayerSpawn.X;
        _player.Y = _level.PlayerSpawn.Y;
    }

    private bool HandleInput(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:    return TryMove(0, -1);
            case ConsoleKey.DownArrow:  return TryMove(0, 1);
            case ConsoleKey.LeftArrow:  return TryMove(-1, 0);
            case ConsoleKey.RightArrow: return TryMove(1, 0);
        }
        switch (key.KeyChar)
        {
            case 'g': case 'G': return TryPickUp();
            case 'i': case 'I': ShowInventory(); return false;
            case '>':           return TryDescend();
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
        var here = _level.ItemAt(nx, ny);
        if (here != null)
            _log.Add($"You see a {here.Item.Name} here. (press g)", ConsoleColor.Cyan);
        if (_level.Tiles[nx, ny].Type == TileType.StairsDown)
            _log.Add("Stairs lead down here. (press >)", ConsoleColor.Yellow);
        return true;
    }

    private void AttackMonster(Monster m)
    {
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
        _log.Add($"You pick up the {ie.Item.Name}.", ConsoleColor.Cyan);
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
            case ItemKind.Weapon:
                _player.Inventory.EquippedWeapon = item;
                _log.Add($"You equip the {item.Name}.", ConsoleColor.Cyan);
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
        _player.Depth++;
        int heal = Math.Min(5, _player.MaxHp - _player.Hp);
        _player.Hp += heal;
        GenerateLevel();
        _log.Add($"You descend to depth {_player.Depth}.", ConsoleColor.Yellow);
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
        Console.WriteLine($"    You reached depth {_player.Depth}.");
        Console.WriteLine($"    Slain monsters: {_player.Kills}");
        Console.WriteLine($"    Final score:    {_player.Score}");
        Console.WriteLine();

        var scores = LoadScores();
        var entry = (score: _player.Score, depth: _player.Depth, kills: _player.Kills, date: DateTime.Now);
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
