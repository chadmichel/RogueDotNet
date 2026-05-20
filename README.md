# Terminal Roguelike

A classic ASCII dungeon crawler in C# / .NET 10. Procedural rooms-and-corridors maps, fog of war, bump-to-attack combat, scaling monsters, endless descent. No external dependencies — just `System.Console`.

## Run it

```bash
dotnet run
```

Requires a real terminal (≥ **80×23**). The game uses `Console.ReadKey`, which doesn't work under piped stdin.

## Controls

| Key      | Action                          |
| -------- | ------------------------------- |
| Arrows   | Move (bump a monster to attack) |
| `Enter`  | Pick up item under you          |
| `f`      | Fire gun at adjacent primate    |
| `i`      | Open inventory (letter to use)  |
| `>`      | Descend stairs                  |
| `q`      | Quit                            |

## Glyphs

`🏃` rogue · `▓/▒` wall · `·/.` floor · `>` stairs down · `!` potion · `+` green energy · `/` weapon · `🔫` fire gun · `🐀 g o T W` monsters (rat → goblin → orc → troll → wraith) · `😈` evil primate

Bright tiles are in your field of view; dim tiles are explored but currently unseen.

## Visual Style

The terminal presentation uses a denser dungeon texture, a side panel with HP and gate status, and strong character colors: cyan for the player, magenta for the evil primate, and muted dungeon tones for the map.

## Primate Gate

Before play, the game shows a rule list one line at a time. Press `Enter` to reveal the next rule; the final `Enter` launches the dungeon.

Stand on a weapon and press `Enter` to collect it. Regular weapons equip automatically when they increase your attack power.

Stand on a green `+` and press `Enter` to restore 50% of your maximum energy. Descending to the next level restores your energy to full.

The first stairway is sealed. After you kill two rats, a stationary evil primate appears on the path to the stairs and blocks the narrow way forward. A `🔫` fire gun appears nearby; stand on it and press `Enter` to collect and equip it. Stand next to the evil primate and press `f` to shoot fire; the third fire blast kills it, unlocks the next level, and records the current score in `highscores.txt`.

## Architecture

```mermaid
graph TD
    Program["Program.cs<br/><i>entry point</i>"] --> Game

    Game["Game<br/><i>turn loop + input dispatch</i>"]

    subgraph Map["Map layer"]
        MapGenerator["MapGenerator<br/><i>rooms + corridors</i>"]
        DungeonLevel["DungeonLevel<br/><i>Tile[,] + actors</i>"]
        Tile["Tile<br/><i>type + visible/explored</i>"]
        Fov["Fov<br/><i>raycast visibility</i>"]
    end

    subgraph Entities
        Entity["Entity<br/><i>base: x,y,glyph,color</i>"]
        Player["Player<br/><i>HP, depth, kills, score</i>"]
        Monster["Monster<br/><i>HP, attack</i>"]
        MonsterFactory["MonsterFactory<br/><i>depth-weighted spawn</i>"]
    end

    subgraph Items
        Item["Item<br/><i>potion / weapon</i>"]
        ItemEntity["ItemEntity<br/><i>item on the floor</i>"]
        Inventory["Inventory<br/><i>items + equipped slot</i>"]
        ItemFactory["ItemFactory"]
    end

    subgraph UI
        Renderer["Renderer<br/><i>double-buffered draw</i>"]
        MessageLog["MessageLog<br/><i>rolling log</i>"]
    end

    Game --> Player
    Game --> DungeonLevel
    Game --> MapGenerator
    Game --> Fov
    Game --> Renderer
    Game --> MessageLog

    MapGenerator -->|builds| DungeonLevel
    MapGenerator --> MonsterFactory
    MapGenerator --> ItemFactory

    DungeonLevel --> Tile
    DungeonLevel -.contains.-> Monster
    DungeonLevel -.contains.-> ItemEntity

    Player --> Inventory
    Inventory --> Item
    ItemEntity --> Item

    MonsterFactory -->|creates| Monster
    ItemFactory -->|creates| ItemEntity

    Fov -.flags.-> Tile

    Player -.extends.-> Entity
    Monster -.extends.-> Entity

    Renderer -.reads.-> DungeonLevel
    Renderer -.reads.-> Player
    Renderer -.reads.-> MessageLog

    classDef core fill:#2d3142,stroke:#bfc0c0,color:#fff
    classDef factory fill:#4f5d75,stroke:#bfc0c0,color:#fff
    class Game,Program core
    class MapGenerator,MonsterFactory,ItemFactory factory
```

### Turn loop

```mermaid
flowchart LR
    A[Compute FOV] --> B[Render frame]
    B --> C[Read key]
    C --> D[Dispatch input]
    D --> E{Action took<br/>a turn?}
    E -->|no| F{Alive &<br/>not quit?}
    E -->|yes| G[Monsters act]
    G --> F
    F -->|yes| A
    F -->|dead| H[Game Over +<br/>high scores]
    F -->|quit| I([Exit])
```

## Project layout

```
Program.cs            entry point
Game.cs               turn loop, input, combat, inventory screen, game over
Map/
  Tile.cs             TileType + visible/explored flags
  DungeonLevel.cs     grid + monster list + item list
  MapGenerator.cs     procedural rooms + L-shaped corridors
  Fov.cs              360-ray raycast field of view
Entities/
  Entity.cs           abstract base
  Player.cs           HP, attack, depth, kills, score, inventory
  Monster.cs          HP, attack, monster kind, optional hit-count health
  MonsterFactory.cs   depth-weighted spawn table
Items/
  Item.cs             Item + ItemEntity + ItemFactory
  Inventory.cs        list + equipped weapon slot
UI/
  Renderer.cs         double-buffered draw (only redraws changed cells)
  MessageLog.cs       rolling buffer of recent actions
```

## Notes

- Combat damage is `attacker.Attack + rng[-1, 1]`, minimum 1.
- Monsters only act while in the player's FOV; outside it they idle.
- Movement is 4-directional for both player and monsters; melee requires orthogonal adjacency.
- `Score = Depth × 100 + Kills × 10`. Top 10 runs persisted to `highscores.txt`.
