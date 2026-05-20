# Agents.md

## Project

A small turn-based ASCII roguelike. Console app, single project, no external dependencies.

- Assembly/csproj: `temp.csproj` (output name `temp`)
- Root namespace: `Rogue`
- Target framework: `net10.0`, `Nullable` enabled, `ImplicitUsings` enabled
- Entry point: `Program.cs` → `new Rogue.Game().Run()`

## Build & run

```
dotnet run
```

The game reads keys from the console, so run it in a real terminal (not a buffered output pane). It expects a terminal at least `TotalWidth x TotalHeight` = 80 x 23 cells. Output encoding is set to UTF-8 in `Program.cs`.

High scores persist to `highscores.txt` in the working directory, one entry per line as `score|depth|kills|date(ISO 8601)`.

## Folder Structure

```
Program.cs           bootstrap
Game.cs              main loop, input dispatch, combat/turn orchestration
Entities/
  Game entity objects with position, health, etc.  
Items/
  Inventory items with type/kind and use effects
Map/
  Map generator and tile definitions
UI/
  Game rendering for inventory/game-over screens
```

## Core loop (`Game.Run`)

1. Compute FOV from player position.
2. Render (map, entities, sidebar, message log).
3. Read one key; `HandleInput` returns whether the action consumed a turn.
4. If a turn was taken and the player is alive, `MonstersAct()` runs every monster on a visible tile (monsters frozen outside FOV — intentional).
5. On death, show game-over screen and persist a high-score entry.

Monster AI: if adjacent to player, attack; otherwise step toward player on the longer axis, falling back to the other axis if blocked.

## Conventions

- Coordinates are `(X, Y)` with `Y` increasing downward (console convention). `Tile[,]` is indexed `[x, y]`.
- All rendering goes through `Renderer` — don't call `Console.Write*` from gameplay code except in the dedicated screens (`ShowInventory`, `ShowGameOver`). After any full-screen takeover, call `_renderer.Reset()` so the next frame fully repaints.
- One shared `Random` lives on `Game._rng` and is threaded into generators/factories. Don't `new Random()` ad hoc — it breaks reproducibility if a seed is ever introduced.
- Monsters and items added to a level should go through `MonsterFactory.Create` / `ItemFactory.Create` so depth-scaling stays in one place.
- `MessageLog.Add` takes an optional `ConsoleColor`; pick colors that match existing usage (red for damage to player, green for kills/heals, cyan for pickups/equip, yellow for prompts, dark gray for no-ops).
- `Renderer` constants (`MapWidth`, `MapHeight`, `SidebarWidth`, `LogHeight`) define the viewport — the map generator is told the viewport size, so resizing the UI also resizes generated levels.

## Adding things

- **New monster**: extend the pool in `MonsterFactory.Create` with a `(weight, factory)` tuple and a `depth >=` gate.
- **New item kind**: add to `ItemKind`, give it fields on `Item`, handle it in `Game.UseItem` and in the inventory detail string in `Game.ShowInventory`. Spawn it from `ItemFactory.Create`.
- **New tile type**: add to `TileType`, update `Tile.IsWalkable` / `BlocksSight` if needed, and add a draw case in `Renderer.DrawMap`.
- **New command key**: add a case in `Game.HandleInput` and return `true` only if it consumes a turn (movement, pickup, descend, attack do; opening inventory or quitting do not).
