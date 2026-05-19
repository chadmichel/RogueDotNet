namespace Rogue.Items;

public class Inventory
{
    public List<Item> Items { get; } = new();
    public Item? EquippedWeapon { get; set; }
    public int Capacity { get; } = 20;

    public bool Add(Item item)
    {
        if (Items.Count >= Capacity) return false;
        Items.Add(item);
        return true;
    }

    public void Remove(Item item)
    {
        Items.Remove(item);
        if (EquippedWeapon == item) EquippedWeapon = null;
    }
}
