namespace Rogue.Entities;

public abstract class Entity
{
    public int X { get; set; }
    public int Y { get; set; }
    public string Glyph { get; set; } = "";
    public ConsoleColor Color { get; set; }
    public string Name { get; set; } = "";
}
