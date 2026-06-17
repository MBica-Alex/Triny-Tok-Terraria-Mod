# TrinyTok Terraria Mod

A **tModLoader (Terraria)** mod that connects to the TrinyTok backend, letting TikTok viewers trigger in-game effects by sending gifts during a livestream.

## How It Works

`TikTok Gift → TrinyTok Backend (Node.js) → TCP :53000 → Terraria Mod → Game Effect`

The mod connects as a TCP client to `127.0.0.1:53000` upon world load and listens for JSON commands from the TrinyTok server. Once a payload is received, the mod queues the action and executes it safely on the main thread.

## Effects

The mod natively supports a wide variety of effects. They are matched by the `code` property sent in the JSON payload.

### Helpful & Buffs
| Profile | Action / Description |
| :--- | :--- |
| **Heals & Resources** | `heal_player`, `give_heart`, `give_mana_star`, `give_gold_coin`, `give_platinum_coin`, `give_life_crystal`, `give_mana_crystal` |
| **Buffs** | `buff_speed`, `buff_regeneration`, `buff_invincibility`, `buff_ironskin`, `buff_night_owl`, `buff_gills`, `buff_shine`, `buff_spelunker`, `buff_featherfall`, `buff_gravitation`, `buff_thorns`, `buff_battle`, `buff_calm` |

### Punishment & Trolls
| Profile | Action / Description |
| :--- | :--- |
| **Damage** | `damage_player` (50 dmg), `damage_player_100` (100 dmg), `explode_player` (150 dmg + grenades) |
| **Lethal / Severe**| `kill_player` (instant kill), `set_half_health` (reduces health to 50%), `drain_mana` |
| **Debuffs** | `debuff_on_fire`, `debuff_frozen`, `debuff_blindness`, `debuff_poison`, `debuff_slow`, `debuff_confusion`, `debuff_cursed`, `debuff_silenced`, `debuff_bleeding`, `debuff_gravity`, `debuff_shimmer` |
| **Annoyances** | `drop_held_item`, `clear_buffs`, `teleport_random` |

### Spawns & Bosses
*NPCs spawned via viewers are automatically given the viewer's name.*

| Profile | Action / Description |
| :--- | :--- |
| **Enemies** | `spawn_slime`, `spawn_zombie`, `spawn_demon_eye`, `spawn_skeleton`, `spawn_pinky`, `spawn_mimic`, `spawn_nymph`, `spawn_bunny`, `spawn_goldfish` |
| **Bosses (Pre-HM)** | `spawn_king_slime`, `spawn_eye_of_cthulhu`, `spawn_eater_of_worlds`, `spawn_brain_of_cthulhu`, `spawn_queen_bee`, `spawn_skeletron`, `spawn_wall_of_flesh` |
| **Bosses (Hardmode)**| `spawn_queen_slime`, `spawn_twins`, `spawn_destroyer`, `spawn_skeletron_prime`, `spawn_plantera`, `spawn_golem`, `spawn_duke_fishron`, `spawn_empress_of_light`, `spawn_moon_lord` |

### Environment & Events
| Profile | Action / Description |
| :--- | :--- |
| **Time & Weather** | `set_time_day`, `set_time_night`, `set_time_noon`, `set_time_midnight`, `toggle_rain`, `start_storm`, `start_sandstorm` |
| **Invasions** | `blood_moon`, `solar_eclipse`, `slime_rain`, `goblin_army`, `pirate_invasion` |

### Items
| Profile | Action / Description |
| :--- | :--- |
| **Swords** | `give_copper_shortsword`, `give_wooden_sword`, `give_nights_edge`, `give_terra_blade`, `give_zenith`, `give_meowmere`, `give_terraprisma` |
| **Utility** | `give_bomb`, `give_dynamite`, `give_healing_potion`, `give_life_fruit`, `give_dirt_block`, `give_angel_wings`, `give_rod_of_discord`, `give_tombstone` |

## Adding Custom Effects

To add new effects, edit the `NetworkSystem.cs` file in the mod source. You can define a new `code` inside the `switch (cmd.code.ToLower())` block inside the `ExecuteCommandOnMainThread` method.

```csharp
case "my_custom_action":
    // Your custom Terraria logic here
    player.AddBuff(Terraria.ID.BuffID.Confused, 60 * 10);
    Main.NewText($"{viewer} triggered a custom action!");
    break;
```

## Protocol

The mod communicates over TCP using JSON payloads.

**Incoming (Server → Mod):**
```json
{ 
  "id": 12345,
  "type": 1,
  "code": "spawn_king_slime", 
  "viewer": "username",
  "duration": 0,
  "count": 1,
  "power": 0
}
```

*Note: In the current architecture, the Terraria mod acts only as a receiver, continuously listening and parsing JSON payloads from the active connection to `127.0.0.1:53000`.*
