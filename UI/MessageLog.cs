namespace Rogue.UI;

public class MessageLog
{
    private readonly LinkedList<(string text, ConsoleColor color)> _messages = new();
    public int MaxMessages { get; set; } = 4;

    public IEnumerable<(string text, ConsoleColor color)> Recent => _messages;

    public void Add(string text, ConsoleColor color = ConsoleColor.Gray)
    {
        _messages.AddLast((text, color));
        while (_messages.Count > MaxMessages) _messages.RemoveFirst();
    }
}
