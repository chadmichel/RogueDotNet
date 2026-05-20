using Rogue.Entities;
using Rogue.Map;

namespace Rogue.UI;

public class Renderer
{
    public const int MapWidth = 60;
    public const int MapHeight = 18;
    public const int SidebarWidth = 20;
    public const int LogHeight = 5;
    public const int TotalWidth = MapWidth + SidebarWidth;
    public const int TotalHeight = MapHeight + LogHeight;

    private readonly string[,] _glyphBuf = new string[TotalWidth, TotalHeight];
    private readonly ConsoleColor[,] _colorBuf = new ConsoleColor[TotalWidth, TotalHeight];
    private readonly string[,] _prevGlyph = new string[TotalWidth, TotalHeight];
    private readonly ConsoleColor[,] _prevColor = new ConsoleColor[TotalWidth, TotalHeight];
    private bool _firstFrame = true;

    public void Render(DungeonLevel level, Player player, MessageLog log)
    {
        ClearBuffer();
        DrawMap(level);
        DrawEntities(level, player);
        DrawSidebar(level, player);
        DrawLog(log);
        Flush();
    }

    public void Reset()
    {
        Console.Clear();
        _firstFrame = true;
    }

    private void ClearBuffer()
    {
        for (int x = 0; x < TotalWidth; x++)
            for (int y = 0; y < TotalHeight; y++)
            {
                _glyphBuf[x, y] = " ";
                _colorBuf[x, y] = ConsoleColor.Gray;
            }
    }

    private void DrawMap(DungeonLevel level)
    {
        int w = Math.Min(MapWidth, level.Width);
        int h = Math.Min(MapHeight, level.Height);
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                var t = level.Tiles[x, y];
                if (!t.Explored) continue;
                string g;
                ConsoleColor c;
                switch (t.Type)
                {
                    case TileType.Wall:
                        g = t.Visible ? "▓" : "▒";
                        c = t.Visible ? ConsoleColor.Gray : ConsoleColor.DarkGray;
                        break;
                    case TileType.Floor:
                        g = t.Visible ? "·" : ".";
                        c = t.Visible ? ConsoleColor.DarkYellow : ConsoleColor.DarkGray;
                        break;
                    case TileType.StairsDown:
                        g = ">";
                        c = t.Visible
                            ? level.StairsLocked ? ConsoleColor.Yellow : ConsoleColor.White
                            : ConsoleColor.DarkGray;
                        break;
                    default:
                        g = " ";
                        c = ConsoleColor.Gray;
                        break;
                }
                _glyphBuf[x, y] = g;
                _colorBuf[x, y] = c;
            }
    }

    private void DrawEntities(DungeonLevel level, Player player)
    {
        foreach (var ie in level.Items)
        {
            if (ie.X >= MapWidth || ie.Y >= MapHeight) continue;
            if (!level.Tiles[ie.X, ie.Y].Visible) continue;
            DrawGlyph(ie.X, ie.Y, ie.Item.Glyph, ie.Item.Color);
        }
        foreach (var m in level.Monsters)
        {
            if (!m.IsAlive) continue;
            if (m.X >= MapWidth || m.Y >= MapHeight) continue;
            if (!level.Tiles[m.X, m.Y].Visible) continue;
            DrawGlyph(m.X, m.Y, m.Glyph, m.Color);
        }
        if (player.X < MapWidth && player.Y < MapHeight)
            DrawGlyph(player.X, player.Y, player.Glyph, player.Hp <= player.MaxHp / 3 ? ConsoleColor.Red : player.Color);
    }

    private void DrawSidebar(DungeonLevel level, Player player)
    {
        int sx = MapWidth;
        DrawDivider();

        WriteAt(sx + 2, 0, "ROGUE", ConsoleColor.Yellow);
        WriteAt(sx + 2, 2, $"Depth {player.Depth}", ConsoleColor.White);
        WriteAt(sx + 2, 3, $"Score {player.Score}", ConsoleColor.Cyan);
        WriteAt(sx + 2, 4, $"Kills {player.Kills}", ConsoleColor.White);

        var hpColor = player.Hp <= player.MaxHp / 3 ? ConsoleColor.Red
                    : player.Hp <= player.MaxHp * 2 / 3 ? ConsoleColor.Yellow
                    : ConsoleColor.Green;
        WriteAt(sx + 2, 6, $"Energy {player.Hp}/{player.MaxHp}", hpColor);
        WriteAt(sx + 2, 7, HealthBar(player.Hp, player.MaxHp, 12), hpColor);
        WriteAt(sx + 2, 8, $"ATK {player.Attack}", ConsoleColor.White);

        WriteAt(sx + 2, 10, "GEAR", ConsoleColor.Yellow);
        WriteAt(sx + 2, 11, Fit(EquippedWeaponText(player), 16), ConsoleColor.Cyan);
        WriteAt(sx + 2, 12, $"Potions {player.Inventory.Items.Count(i => i.Kind == Items.ItemKind.HealingPotion)}", ConsoleColor.Red);

        WriteAt(sx + 2, 14, $"GATE {(level.StairsLocked ? "sealed" : "open")}",
            level.StairsLocked ? ConsoleColor.DarkYellow : ConsoleColor.Green);

        WriteAt(sx + 2, 15, "↑↓←→ move", ConsoleColor.Gray);
        WriteAt(sx + 2, 16, "Enter pick f fire", ConsoleColor.Gray);
        WriteAt(sx + 2, 17, "i bag > q quit", ConsoleColor.Gray);
    }

    private void DrawLog(MessageLog log)
    {
        int y = MapHeight;
        WriteAt(0, y, new string('─', TotalWidth), ConsoleColor.DarkGray);
        int row = 0;
        foreach (var (text, color) in log.Recent)
        {
            if (row >= LogHeight - 1) break;
            WriteAt(0, y + 1 + row, text, color);
            row++;
        }
    }

    private void DrawDivider()
    {
        for (int y = 0; y < MapHeight; y++)
        {
            _glyphBuf[MapWidth, y] = "│";
            _colorBuf[MapWidth, y] = ConsoleColor.DarkGray;
        }
    }

    private void DrawGlyph(int x, int y, string glyph, ConsoleColor color)
    {
        if (x < 0 || y < 0 || x >= MapWidth || y >= MapHeight || string.IsNullOrEmpty(glyph))
            return;

        _glyphBuf[x, y] = glyph;
        _colorBuf[x, y] = color;

        if (GlyphWidth(glyph) > 1 && x + 1 < MapWidth)
        {
            _glyphBuf[x + 1, y] = "";
            _colorBuf[x + 1, y] = color;
        }
    }

    private static string HealthBar(int hp, int maxHp, int width)
    {
        int filled = maxHp <= 0 ? 0 : (int)Math.Round(width * Math.Clamp(hp, 0, maxHp) / (double)maxHp);
        return "[" + new string('█', filled) + new string('░', width - filled) + "]";
    }

    private static string Fit(string text, int width)
    {
        if (text.Length <= width) return text;
        if (width <= 1) return text[..width];
        return text[..(width - 1)] + "…";
    }

    private static string EquippedWeaponText(Player player)
    {
        var weapon = player.Inventory.EquippedWeapon;
        if (weapon == null) return "fists";
        return weapon.IsFireWeapon
            ? $"{weapon.Name} {weapon.FireCharges}/{weapon.MaxFireCharges}"
            : weapon.Name;
    }

    private void WriteAt(int x, int y, string text, ConsoleColor color)
    {
        if (y < 0 || y >= TotalHeight) return;
        int col = x;
        for (int i = 0; i < text.Length; i++)
        {
            if (col >= TotalWidth) break;
            string glyph;
            int width;
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                glyph = text.Substring(i, 2);
                width = 2;
                i++;
            }
            else
            {
                glyph = text[i].ToString();
                width = 1;
            }
            if (col >= 0)
            {
                _glyphBuf[col, y] = glyph;
                _colorBuf[col, y] = color;
                if (width > 1 && col + 1 < TotalWidth)
                {
                    _glyphBuf[col + 1, y] = "";
                    _colorBuf[col + 1, y] = color;
                }
            }
            col += width;
        }
    }

    private static int GlyphWidth(string glyph) => glyph.Length > 1 ? 2 : 1;

    private void Flush()
    {
        for (int y = 0; y < TotalHeight; y++)
        {
            for (int x = 0; x < TotalWidth; x++)
            {
                if (!_firstFrame && _glyphBuf[x, y] == _prevGlyph[x, y] && _colorBuf[x, y] == _prevColor[x, y])
                    continue;
                if (_glyphBuf[x, y] == "")
                {
                    _prevGlyph[x, y] = _glyphBuf[x, y];
                    _prevColor[x, y] = _colorBuf[x, y];
                    continue;
                }
                Console.SetCursorPosition(x, y);
                Console.ForegroundColor = _colorBuf[x, y];
                Console.Write(_glyphBuf[x, y]);
                _prevGlyph[x, y] = _glyphBuf[x, y];
                _prevColor[x, y] = _colorBuf[x, y];
            }
        }
        _firstFrame = false;
        Console.ForegroundColor = ConsoleColor.Gray;
    }
}
