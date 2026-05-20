using Rogue.Entities;
using Rogue.Map;

namespace Rogue.UI;

public class Renderer
{
    public const int MapWidth = 30;
    public const int MapHeight = 18;
    public const int CellsPerTile = 2;
    public const int MapPixelWidth = MapWidth * CellsPerTile;
    public const int SidebarWidth = 20;
    public const int LogHeight = 5;
    public const int TotalWidth = MapPixelWidth + SidebarWidth;
    public const int TotalHeight = MapHeight + LogHeight;

    private readonly string[,] _glyphBuf = new string[TotalWidth, TotalHeight];
    private readonly ConsoleColor[,] _colorBuf = new ConsoleColor[TotalWidth, TotalHeight];
    private readonly string[,] _prevGlyph = new string[TotalWidth, TotalHeight];
    private readonly ConsoleColor[,] _prevColor = new ConsoleColor[TotalWidth, TotalHeight];
    private bool _firstFrame = true;

    public Renderer()
    {
        for (int x = 0; x < TotalWidth; x++)
            for (int y = 0; y < TotalHeight; y++)
            {
                _glyphBuf[x, y] = " ";
                _prevGlyph[x, y] = " ";
            }
    }

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

    public void DrawTransient(int tileX, int tileY, string glyph, ConsoleColor color)
    {
        int px = tileX * CellsPerTile;
        if (px < 0 || px + 1 >= TotalWidth || tileY < 0 || tileY >= TotalHeight) return;
        Console.SetCursorPosition(px, tileY);
        Console.ForegroundColor = color;
        Console.Write(glyph);
        _prevGlyph[px, tileY] = glyph;
        _prevColor[px, tileY] = color;
        _prevGlyph[px + 1, tileY] = "";
        _prevColor[px + 1, tileY] = color;
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

    private void PutTile(int tx, int ty, string glyph, ConsoleColor color)
    {
        int px = tx * CellsPerTile;
        if (px < 0 || px + 1 >= TotalWidth || ty < 0 || ty >= TotalHeight) return;
        _glyphBuf[px, ty] = glyph;
        _colorBuf[px, ty] = color;
        _glyphBuf[px + 1, ty] = "";
        _colorBuf[px + 1, ty] = color;
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
                        g = "██";
                        c = t.Visible ? ConsoleColor.DarkCyan : ConsoleColor.DarkBlue;
                        break;
                    case TileType.Floor:
                        g = "··";
                        c = t.Visible ? ConsoleColor.DarkYellow : ConsoleColor.DarkGray;
                        break;
                    case TileType.StairsDown:
                        g = ">>";
                        c = t.Visible ? ConsoleColor.Yellow : ConsoleColor.DarkYellow;
                        break;
                    case TileType.StairsUp:
                        g = "<<";
                        c = t.Visible ? ConsoleColor.White : ConsoleColor.Gray;
                        break;
                    case TileType.Fountain:
                        g = "⛲";
                        c = t.Visible ? ConsoleColor.Cyan : ConsoleColor.DarkCyan;
                        break;
                    default:
                        continue;
                }
                PutTile(x, y, g, c);
            }
    }

    private void DrawEntities(DungeonLevel level, Player player)
    {
        foreach (var ie in level.Items)
        {
            if (ie.X >= MapWidth || ie.Y >= MapHeight) continue;
            if (!level.Tiles[ie.X, ie.Y].Visible) continue;
            PutTile(ie.X, ie.Y, ie.Item.Glyph, ie.Item.Color);
        }
        foreach (var m in level.Monsters)
        {
            if (!m.IsAlive) continue;
            if (m.X >= MapWidth || m.Y >= MapHeight) continue;
            if (!level.Tiles[m.X, m.Y].Visible) continue;
            PutTile(m.X, m.Y, m.Glyph, m.Color);
        }
        if (player.X < MapWidth && player.Y < MapHeight)
            PutTile(player.X, player.Y, player.Glyph, player.Color);
    }

    private void DrawSidebar(Player player)
    {
        int sx = MapPixelWidth;
        WriteAt(sx + 1, 0, "-- STATUS --", ConsoleColor.Yellow);
        WriteAt(sx + 1, 1, $"Depth: {player.Depth} (max {player.MaxDepth})", ConsoleColor.White);
        var hpColor = player.Hp <= player.MaxHp / 3 ? ConsoleColor.Red
                    : player.Hp <= player.MaxHp * 2 / 3 ? ConsoleColor.Yellow
                    : ConsoleColor.Green;
        WriteAt(sx + 1, 2, $"HP:    {player.Hp}/{player.MaxHp}", hpColor);
        WriteAt(sx + 1, 3, $"ATK:   {player.Attack}", ConsoleColor.White);
        WriteAt(sx + 1, 4, $"Kills: {player.Kills}", ConsoleColor.White);
        WriteAt(sx + 1, 5, $"Score: {player.Score}", ConsoleColor.Cyan);

        WriteAt(sx + 1, 7, "-- KEYS --", ConsoleColor.Yellow);
        WriteAt(sx + 1, 8,  "arrows: move", ConsoleColor.Gray);
        WriteAt(sx + 1, 9,  "g: pick up",   ConsoleColor.Gray);
        WriteAt(sx + 1, 10, "i: inventory", ConsoleColor.Gray);
        WriteAt(sx + 1, 11, "z: zap wand",  ConsoleColor.Gray);
        WriteAt(sx + 1, 12, ">: descend",   ConsoleColor.Gray);
        WriteAt(sx + 1, 13, "<: ascend",    ConsoleColor.Gray);
        WriteAt(sx + 1, 14, "q: quit",      ConsoleColor.Gray);

        WriteAt(sx + 1, 15, "Weapon:", ConsoleColor.Yellow);
        WriteAt(sx + 1, 16, player.Inventory.EquippedWeapon?.DisplayName ?? "(fists)", ConsoleColor.Cyan);
        WriteAt(sx + 1, 17, $"Wand: {player.Inventory.EquippedWand?.DisplayName ?? "(none)"}", ConsoleColor.Magenta);
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
            _glyphBuf[x + i, y] = text[i].ToString();
            _colorBuf[x + i, y] = color;
        }
    }

    private void Flush()
    {
        for (int y = 0; y < TotalHeight; y++)
        {
            for (int x = 0; x < TotalWidth; x++)
            {
                if (!_firstFrame
                    && _glyphBuf[x, y] == _prevGlyph[x, y]
                    && _colorBuf[x, y] == _prevColor[x, y])
                    continue;
                if (_glyphBuf[x, y].Length > 0)
                {
                    Console.SetCursorPosition(x, y);
                    Console.ForegroundColor = _colorBuf[x, y];
                    Console.Write(_glyphBuf[x, y]);
                }
                _prevGlyph[x, y] = _glyphBuf[x, y];
                _prevColor[x, y] = _colorBuf[x, y];
            }
        }
        _firstFrame = false;
        Console.ForegroundColor = ConsoleColor.Gray;
    }
}
