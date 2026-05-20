using Rogue.Entities;
using Rogue.Items;

namespace Rogue.Map;

public class MapGenerator
{
    private readonly Random _rng;
    public MapGenerator(Random rng) { _rng = rng; }

    public DungeonLevel Generate(int width, int height, int depth, bool allowBossSpawn)
    {
        var level = new DungeonLevel(width, height);
        var rooms = new List<Room>();
        const int maxAttempts = 30;
        const int minSize = 4, maxSize = 9;

        for (int i = 0; i < maxAttempts; i++)
        {
            int w = _rng.Next(minSize, maxSize + 1);
            int h = _rng.Next(minSize, maxSize + 1);
            int x = _rng.Next(1, width - w - 1);
            int y = _rng.Next(1, height - h - 1);
            var room = new Room(x, y, w, h);

            bool overlap = false;
            foreach (var other in rooms)
                if (room.Intersects(other)) { overlap = true; break; }
            if (overlap) continue;

            CarveRoom(level, room);
            if (rooms.Count > 0)
            {
                var (px, py) = rooms[^1].Center;
                var (cx, cy) = room.Center;
                if (_rng.Next(2) == 0)
                {
                    CarveHTunnel(level, px, cx, py);
                    CarveVTunnel(level, py, cy, cx);
                }
                else
                {
                    CarveVTunnel(level, py, cy, px);
                    CarveHTunnel(level, px, cx, cy);
                }
            }
            rooms.Add(room);
        }

        level.PlayerSpawn = rooms[0].Center;

        var stairs = rooms[^1].Center;
        level.Tiles[stairs.X, stairs.Y].Type = TileType.StairsDown;
        level.StairsDown = stairs;

        if (depth > 1)
        {
            var stairsUp = rooms[0].Center;
            level.Tiles[stairsUp.X, stairsUp.Y].Type = TileType.StairsUp;
            level.StairsUp = stairsUp;
            level.HasStairsUp = true;
        }

        bool shouldSpawnBoss = allowBossSpawn && depth > 3 && _rng.Next(100) < 15;
        if (shouldSpawnBoss && rooms.Count > 1)
        {
            int roomIndex = _rng.Next(1, rooms.Count);
            var (bx, by) = rooms[roomIndex].RandomPoint(_rng);
            level.Monsters.Add(MonsterFactory.CreateBoss(bx, by));
        }

        for (int i = 1; i < rooms.Count; i++)
        {
            if (_rng.Next(100) < 20)
            {
                var (fx, fy) = rooms[i].RandomPoint(_rng);
                if ((fx, fy) != stairs)
                    level.Tiles[fx, fy].Type = TileType.Fountain;
            }

            int monsterCount = _rng.Next(0, 2 + depth / 2);
            for (int j = 0; j < monsterCount; j++)
            {
                var (mx, my) = rooms[i].RandomPoint(_rng);
                if (level.MonsterAt(mx, my) == null && (mx, my) != stairs)
                    level.Monsters.Add(MonsterFactory.Create(mx, my, depth, _rng));
            }
            if (_rng.Next(100) < 60)
            {
                var (ix, iy) = rooms[i].RandomPoint(_rng);
                if (level.ItemAt(ix, iy) == null)
                    level.Items.Add(ItemFactory.Create(ix, iy, depth, _rng));
            }
            if (_rng.Next(100) < 20)
            {
                var (ix, iy) = rooms[i].RandomPoint(_rng);
                if (level.ItemAt(ix, iy) == null)
                    level.Items.Add(ItemFactory.Create(ix, iy, depth, _rng));
            }
        }

        return level;
    }

    private static void CarveRoom(DungeonLevel level, Room room)
    {
        for (int x = room.X; x < room.X + room.Width; x++)
            for (int y = room.Y; y < room.Y + room.Height; y++)
                level.Tiles[x, y].Type = TileType.Floor;
    }

    private static void CarveHTunnel(DungeonLevel level, int x1, int x2, int y)
    {
        for (int x = Math.Min(x1, x2); x <= Math.Max(x1, x2); x++)
            level.Tiles[x, y].Type = TileType.Floor;
    }

    private static void CarveVTunnel(DungeonLevel level, int y1, int y2, int x)
    {
        for (int y = Math.Min(y1, y2); y <= Math.Max(y1, y2); y++)
            level.Tiles[x, y].Type = TileType.Floor;
    }

    private readonly record struct Room(int X, int Y, int Width, int Height)
    {
        public (int X, int Y) Center => (X + Width / 2, Y + Height / 2);

        public bool Intersects(Room other) =>
            X <= other.X + other.Width && X + Width >= other.X &&
            Y <= other.Y + other.Height && Y + Height >= other.Y;

        public (int X, int Y) RandomPoint(Random rng) =>
            (rng.Next(X + 1, X + Width - 1), rng.Next(Y + 1, Y + Height - 1));
    }
}
