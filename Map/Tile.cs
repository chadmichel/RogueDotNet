namespace Rogue.Map;

public enum TileType { Wall, Floor, StairsDown, StairsUp, Fountain }

public struct Tile
{
    public TileType Type;
    public bool Visible;
    public bool Explored;

    public bool IsWalkable => Type != TileType.Wall;
    public bool BlocksSight => Type == TileType.Wall;
}
