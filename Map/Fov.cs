namespace Rogue.Map;

public static class Fov
{
    public static void Compute(DungeonLevel level, int px, int py, int radius)
    {
        level.ResetVisibility();
        if (!level.InBounds(px, py)) return;
        level.Tiles[px, py].Visible = true;
        level.Tiles[px, py].Explored = true;

        const int rays = 360;
        for (int i = 0; i < rays; i++)
        {
            double angle = (i * Math.PI * 2.0) / rays;
            double dx = Math.Cos(angle);
            double dy = Math.Sin(angle);
            double x = px + 0.5;
            double y = py + 0.5;
            for (int step = 0; step < radius; step++)
            {
                x += dx;
                y += dy;
                int ix = (int)x, iy = (int)y;
                if (!level.InBounds(ix, iy)) break;
                level.Tiles[ix, iy].Visible = true;
                level.Tiles[ix, iy].Explored = true;
                if (level.Tiles[ix, iy].BlocksSight) break;
            }
        }
    }
}
