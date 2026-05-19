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

    private readonly char[,] _glyphBuf = new char[TotalWidth, TotalHeight];
    private readonly ConsoleColor[,] _colorBuf = new ConsoleColor[TotalWidth, TotalHeight];
    private readonly char[,] _prevGlyph = new char[TotalWidth, TotalHeight];
    private readonly ConsoleColor[,] _prevColor = new ConsoleColor[TotalWidth, TotalHeight];
    private bool _firstFrame = true;

    public void Render(DungeonLevel level, Player player, MessageLog log)
    {
        ClearBuffer();
        DrawMap(level);
        DrawEntities(level, player);
        DrawSidebar(player);
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
                _glyphBuf[x, y] = ' ';
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
                char g;
                ConsoleColor c;
                switch (t.Type)
                {
                    case TileType.Wall:
                        g = '#';
                        c = t.Visible ? ConsoleColor.Gray : ConsoleColor.DarkGray;
                        break;
                    case TileType.Floor:
                        g = '.';
                        c = t.Visible ? ConsoleColor.DarkYellow : ConsoleColor.DarkGray;
                        break;
                    case TileType.StairsDown:
                        g = '>';
                        c = t.Visible ? ConsoleColor.White : ConsoleColor.DarkGray;
                        break;
                    default:
                        g = ' ';
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
            _glyphBuf[ie.X, ie.Y] = ie.Item.Glyph;
            _colorBuf[ie.X, ie.Y] = ie.Item.Color;
        }
        foreach (var m in level.Monsters)
        {
            if (!m.IsAlive) continue;
            if (m.X >= MapWidth || m.Y >= MapHeight) continue;
            if (!level.Tiles[m.X, m.Y].Visible) continue;
            _glyphBuf[m.X, m.Y] = m.Glyph;
            _colorBuf[m.X, m.Y] = m.Color;
        }
        if (player.X < MapWidth && player.Y < MapHeight)
        {
            _glyphBuf[player.X, player.Y] = player.Glyph;
            _colorBuf[player.X, player.Y] = player.Color;
        }
    }

    private void DrawSidebar(Player player)
    {
        int sx = MapWidth;
        WriteAt(sx + 1, 0, "-- STATUS --", ConsoleColor.Yellow);
        WriteAt(sx + 1, 2, $"Depth: {player.Depth}", ConsoleColor.White);
        var hpColor = player.Hp <= player.MaxHp / 3 ? ConsoleColor.Red
                    : player.Hp <= player.MaxHp * 2 / 3 ? ConsoleColor.Yellow
                    : ConsoleColor.Green;
        WriteAt(sx + 1, 3, $"HP:    {player.Hp}/{player.MaxHp}", hpColor);
        WriteAt(sx + 1, 4, $"ATK:   {player.Attack}", ConsoleColor.White);
        WriteAt(sx + 1, 5, $"Kills: {player.Kills}", ConsoleColor.White);
        WriteAt(sx + 1, 6, $"Score: {player.Score}", ConsoleColor.Cyan);

        WriteAt(sx + 1, 8, "-- KEYS --", ConsoleColor.Yellow);
        WriteAt(sx + 1, 9,  "arrows: move", ConsoleColor.Gray);
        WriteAt(sx + 1, 10, "g: pick up",   ConsoleColor.Gray);
        WriteAt(sx + 1, 11, "i: inventory", ConsoleColor.Gray);
        WriteAt(sx + 1, 12, ">: descend",   ConsoleColor.Gray);
        WriteAt(sx + 1, 13, "q: quit",      ConsoleColor.Gray);

        WriteAt(sx + 1, 15, "Weapon:", ConsoleColor.Yellow);
        WriteAt(sx + 1, 16, player.Inventory.EquippedWeapon?.Name ?? "(fists)", ConsoleColor.Cyan);
        WriteAt(sx + 1, 17, $"Potions: {player.Inventory.Items.Count(i => i.Kind == Items.ItemKind.HealingPotion)}", ConsoleColor.Red);
    }

    private void DrawLog(MessageLog log)
    {
        int y = MapHeight;
        WriteAt(0, y, new string('-', TotalWidth), ConsoleColor.DarkGray);
        int row = 0;
        foreach (var (text, color) in log.Recent)
        {
            if (row >= LogHeight - 1) break;
            WriteAt(0, y + 1 + row, text, color);
            row++;
        }
    }

    private void WriteAt(int x, int y, string text, ConsoleColor color)
    {
        if (y < 0 || y >= TotalHeight) return;
        for (int i = 0; i < text.Length && x + i < TotalWidth; i++)
        {
            if (x + i < 0) continue;
            _glyphBuf[x + i, y] = text[i];
            _colorBuf[x + i, y] = color;
        }
    }

    private void Flush()
    {
        for (int y = 0; y < TotalHeight; y++)
        {
            for (int x = 0; x < TotalWidth; x++)
            {
                if (!_firstFrame && _glyphBuf[x, y] == _prevGlyph[x, y] && _colorBuf[x, y] == _prevColor[x, y])
                    continue;
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
