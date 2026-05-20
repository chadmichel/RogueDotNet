using Rogue.Entities;
using Rogue.Items;

namespace Rogue.Map;

public class DungeonLevel
{
    public int Width { get; }
    public int Height { get; }
    public Tile[,] Tiles { get; }
    public List<Monster> Monsters { get; } = new();
    public List<ItemEntity> Items { get; } = new();
    public (int X, int Y) StairsDown { get; set; }
    public (int X, int Y) StairsUp { get; set; }
    public bool HasStairsUp { get; set; }
    public (int X, int Y) PlayerSpawn { get; set; }

    public DungeonLevel(int width, int height)
    {
        Width = width;
        Height = height;
        Tiles = new Tile[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                Tiles[x, y] = new Tile { Type = TileType.Wall };
    }

    public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;
    public bool IsWalkable(int x, int y) => InBounds(x, y) && Tiles[x, y].IsWalkable;

    public Monster? MonsterAt(int x, int y)
    {
        foreach (var m in Monsters)
            if (m.IsAlive && m.X == x && m.Y == y) return m;
        return null;
    }

    public ItemEntity? ItemAt(int x, int y)
    {
        foreach (var i in Items)
            if (i.X == x && i.Y == y) return i;
        return null;
    }

    public void ResetVisibility()
    {
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                Tiles[x, y].Visible = false;
    }
}
