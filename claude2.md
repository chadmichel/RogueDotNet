# claude2.md

Guidance for Claude Code when working in this repo.

## Project

Terminal-based ASCII roguelike written in C# / .NET 10. Single console app, no external dependencies — just `System.Console` for input, color, and cursor positioning.

Genre: classic procedural dungeon crawler. Endless survival, score by depth + kills.

## Build & run

```bash
dotnet build         # compile
dotnet run           # play (requires a real TTY — Console.ReadKey)
```

Project file: [temp.csproj](temp.csproj). Target framework `net10.0`, `ImplicitUsings` and `Nullable` both enabled.

High scores persist to `highscores.txt` next to the binary (top 10).

## Architecture

Top-level [Program.cs](Program.cs) just instantiates and runs `Rogue.Game`.

```
Program.cs            entry point
Game.cs               turn loop, input dispatch, combat, inventory UI, game-over
Map/
  Tile.cs             TileType enum (Wall/Floor/StairsDown) + Visible/Explored flags
  DungeonLevel.cs     2D Tile grid + monster list + item list per floor
  MapGenerator.cs     rooms + L-shaped corridors; spawns monsters/items
  Fov.cs              360-ray raycast FOV, marks Visible + Explored
Entities/
  Entity.cs           abstract base: X, Y, Glyph, Color, Name
  Player.cs           HP, attack, depth, kills, inventory; Score = Depth*100 + Kills*10
  Monster.cs          HP, attack
  MonsterFactory.cs   depth-weighted spawn table (rat → goblin → orc → troll → wraith)
Items/
  Item.cs             Item + ItemEntity (item on the floor) + ItemFactory
  Inventory.cs        item list + EquippedWeapon slot
UI/
  Renderer.cs         double-buffered draw (only diffs); map + sidebar + log layout
  MessageLog.cs       rolling buffer of recent action lines
```

### Turn loop ([Game.cs:31](Game.cs#L31))

1. Compute FOV from player position
2. Render frame
3. `Console.ReadKey(true)` — block for input
4. Dispatch via `HandleInput`; player action returns `tookTurn`
5. If turn was taken and player alive, `MonstersAct()`
6. Loop until player dies or `q`

### Rendering ([UI/Renderer.cs](UI/Renderer.cs))

- Fixed layout: **80×23**. Map area 60×18, sidebar 20 wide, log 5 tall at bottom.
- Two buffers (current + previous). `Flush()` only redraws cells that changed — keeps the terminal from flickering.
- After clearing screen externally (e.g. inventory overlay), call `_renderer.Reset()` to force a full repaint next frame.

### FOV ([Map/Fov.cs](Map/Fov.cs))

Cheap-and-dirty: 360 rays from the player, each marches up to `radius` steps marking tiles `Visible` and `Explored`, stopping at the first wall (the wall itself is marked). Good enough for an 80-col map; replace with shadowcasting if perf or symmetry becomes an issue.

### Combat & AI

- **Bump-to-attack**: walking into a monster's tile triggers a melee swing. Damage = `attacker.Attack + rng[-1, 1]`, minimum 1.
- **Monsters only act when visible**. If orthogonally adjacent, they attack; otherwise they step toward the player on the dominant axis, falling back to the other axis if blocked. Diagonal movement is intentionally disallowed for both player and monsters.
- Difficulty scales with `_player.Depth` via `MonsterFactory` (gates by depth, weighted pool).

## Conventions

- Top-level statements in [Program.cs](Program.cs); everything else in `Rogue.*` namespaces matching the folder layout.
- Nullable reference types are on. Use `null!` for fields initialized in `Run()` (e.g. `_level`).
- `Tile` is a `struct` — mutated in-place via `Tiles[x, y].Type = ...`. Don't capture tiles into local variables before mutating; you'll mutate a copy.
- No emojis in source or logs. Box-drawing replaced with ASCII dashes/hashes for portability.

## Gotchas

- **`Console.ReadKey(true)` requires a real TTY.** It will throw under piped stdin or non-interactive harnesses, so the game can't be smoke-tested from a script — only by running it in a terminal.
- **Terminal must be ≥80×23.** Smaller and `Console.SetCursorPosition` will throw `ArgumentOutOfRangeException`.
- `'>'` (descend stairs) is matched on `key.KeyChar`, not `ConsoleKey.OemPeriod` + Shift — keyboard layouts differ.
- High scores file format is pipe-delimited: `score|depth|kills|iso-date`. Trivial to corrupt by hand; malformed lines are skipped silently in `LoadScores`.
