using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace TrinyTokMod
{
    public class NetworkSystem : ModSystem
    {
        private CancellationTokenSource _cts;
        
        // Queue for safely passing actions to the main thread
        private static readonly ConcurrentQueue<CommandPayload> _commandQueue = new();

        public override void OnWorldLoad()
        {
            if (Main.netMode == Terraria.ID.NetmodeID.Server) return;

            _cts = new CancellationTokenSource();
            _ = ConnectionLoopAsync(_cts.Token);
            TrinyTokMod.Instance.Logger.Info("TrinyTok Client started, looking for Node.js server on 53000...");
        }

        public override void OnWorldUnload()
        {
            _cts?.Cancel();
        }

        private async Task ConnectionLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using (TcpClient client = new TcpClient())
                    {
                        await client.ConnectAsync(IPAddress.Loopback, 53000);
                        TrinyTokMod.Instance.Logger.Info("Connected to Triny-Tok server!");
                        
                        using (var stream = client.GetStream())
                        using (var reader = new StreamReader(stream))
                        {
                            string currentData = "";
                            char[] buffer = new char[1024];

                            while (!token.IsCancellationRequested)
                            {
                                int bytesRead = await reader.ReadAsync(buffer, 0, buffer.Length);
                                if (bytesRead == 0) break; 

                                for (int i = 0; i < bytesRead; i++)
                                {
                                    char c = buffer[i];
                                    if (c == '\0')
                                    {
                                        ProcessJsonPayload(currentData);
                                        currentData = "";
                                    }
                                    else
                                    {
                                        currentData += c;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    await Task.Delay(3000, token);
                }
            }
        }

        private void ProcessJsonPayload(string json)
        {
            try
            {
                var payload = JsonSerializer.Deserialize<CommandPayload>(json);
                if (payload != null && !string.IsNullOrEmpty(payload.code))
                {
                    _commandQueue.Enqueue(payload);
                    TrinyTokMod.Instance.Logger.Info($"Queued command: {payload.code}");
                }
            }
            catch (Exception ex)
            {
                TrinyTokMod.Instance.Logger.Error($"JSON Parse error: {ex.Message} - Data: {json}");
            }
        }

        public override void PostUpdateEverything()
        {
            if (Main.netMode == Terraria.ID.NetmodeID.Server || Main.myPlayer < 0)
                return;

            Player player = Main.LocalPlayer;

            while (_commandQueue.TryDequeue(out var cmd))
            {
                ExecuteCommandOnMainThread(player, cmd);
            }
        }

        private void ExecuteCommandOnMainThread(Player player, CommandPayload cmd)
        {
            string viewer = string.IsNullOrEmpty(cmd.viewer) ? "Someone" : cmd.viewer;
            Main.NewText($"[TikTok] {viewer} triggered {cmd.code}!", 255, 0, 255);

            int x = (int)player.Center.X;
            int y = (int)player.Center.Y;

            switch (cmd.code.ToLower())
            {
                // ==== HELPFUL ====
                case "heal_player":
                    player.statLife = player.statLifeMax2;
                    player.HealEffect(player.statLifeMax2, true);
                    break;
                case "give_heart":
                    player.QuickSpawnItem(player.GetSource_Misc("TrinyTok"), Terraria.ID.ItemID.Heart, 1);
                    break;
                case "give_mana_star":
                    player.QuickSpawnItem(player.GetSource_Misc("TrinyTok"), Terraria.ID.ItemID.Star, 1);
                    break;
                case "give_gold_coin":
                    player.QuickSpawnItem(player.GetSource_Misc("TrinyTok"), Terraria.ID.ItemID.GoldCoin, 1);
                    break;
                case "give_platinum_coin":
                    player.QuickSpawnItem(player.GetSource_Misc("TrinyTok"), Terraria.ID.ItemID.PlatinumCoin, 1);
                    break;
                case "give_life_crystal":
                    player.QuickSpawnItem(player.GetSource_Misc("TrinyTok"), Terraria.ID.ItemID.LifeCrystal, 1);
                    break;
                case "give_mana_crystal":
                    player.QuickSpawnItem(player.GetSource_Misc("TrinyTok"), Terraria.ID.ItemID.ManaCrystal, 1);
                    break;

                // ==== BUFFS ====
                case "buff_speed":
                    player.AddBuff(Terraria.ID.BuffID.Swiftness, 60 * 30);
                    break;
                case "buff_regeneration":
                    player.AddBuff(Terraria.ID.BuffID.Regeneration, 60 * 30);
                    break;
                case "buff_invincibility":
                    player.immune = true;
                    player.immuneTime = 60 * 5;
                    break;
                case "buff_ironskin":
                    player.AddBuff(Terraria.ID.BuffID.Ironskin, 60 * 30);
                    break;
                case "buff_night_owl":
                    player.AddBuff(Terraria.ID.BuffID.NightOwl, 60 * 30);
                    break;
                case "buff_gills":
                    player.AddBuff(Terraria.ID.BuffID.Gills, 60 * 30);
                    break;
                case "buff_shine":
                    player.AddBuff(Terraria.ID.BuffID.Shine, 60 * 30);
                    break;
                case "buff_spelunker":
                    player.AddBuff(Terraria.ID.BuffID.Spelunker, 60 * 30);
                    break;
                case "buff_featherfall":
                    player.AddBuff(Terraria.ID.BuffID.Featherfall, 60 * 30);
                    break;
                case "buff_gravitation":
                    player.AddBuff(Terraria.ID.BuffID.Gravitation, 60 * 30);
                    break;
                case "buff_thorns":
                    player.AddBuff(Terraria.ID.BuffID.Thorns, 60 * 30);
                    break;
                case "buff_battle":
                    player.AddBuff(Terraria.ID.BuffID.Battle, 60 * 30);
                    break;
                case "buff_calm":
                    player.AddBuff(Terraria.ID.BuffID.Calm, 60 * 30);
                    break;

                // ==== TROLL ====
                case "kill_player":
                    player.KillMe(Terraria.DataStructures.PlayerDeathReason.ByCustomReason($"{viewer} sent you to the shadow realm!"), 1000, 0);
                    break;
                case "damage_player":
                    player.Hurt(Terraria.DataStructures.PlayerDeathReason.ByCustomReason($"{viewer} bit you!"), 50, 0);
                    break;
                case "damage_player_100":
                    player.Hurt(Terraria.DataStructures.PlayerDeathReason.ByCustomReason($"{viewer} smashed you!"), 100, 0);
                    break;
                case "explode_player":
                    player.Hurt(Terraria.DataStructures.PlayerDeathReason.ByCustomReason($"{viewer} blew you up!"), 150, 0);
                    for (int i = 0; i < 3; i++)
                        Terraria.Projectile.NewProjectile(player.GetSource_Misc("TrinyTok"), player.Center.X + Main.rand.Next(-40, 40), player.Center.Y + Main.rand.Next(-40, 40), 0, 0, Terraria.ID.ProjectileID.Grenade, 40, 4f, Main.myPlayer);
                    break;
                case "debuff_on_fire":
                    player.AddBuff(Terraria.ID.BuffID.OnFire, 60 * 5);
                    break;
                case "debuff_frozen":
                    player.AddBuff(Terraria.ID.BuffID.Frozen, 60 * 3);
                    break;
                case "debuff_blindness":
                    player.AddBuff(Terraria.ID.BuffID.Darkness, 60 * 5);
                    break;
                case "debuff_poison":
                    player.AddBuff(Terraria.ID.BuffID.Poisoned, 60 * 10);
                    break;
                case "debuff_slow":
                    player.AddBuff(Terraria.ID.BuffID.Slow, 60 * 10);
                    break;
                case "debuff_confusion":
                    player.AddBuff(Terraria.ID.BuffID.Confused, 60 * 5);
                    break;
                case "debuff_cursed":
                    player.AddBuff(Terraria.ID.BuffID.Cursed, 60 * 3);
                    break;
                case "debuff_silenced":
                    player.AddBuff(Terraria.ID.BuffID.Silenced, 60 * 10);
                    break;
                case "debuff_bleeding":
                    player.AddBuff(Terraria.ID.BuffID.Bleeding, 60 * 10);
                    break;
                case "debuff_gravity":
                    player.AddBuff(Terraria.ID.BuffID.Gravitation, 60 * 10);
                    break;
                case "drop_held_item":
                    if (player.HeldItem != null && !player.HeldItem.IsAir)
                    {
                        player.DropItem(player.GetSource_Misc("TrinyTok"), player.Center, ref player.inventory[player.selectedItem]);
                    }
                    break;
                case "clear_buffs":
                    for (int i = 0; i < Player.MaxBuffs; i++)
                    {
                        if (player.buffTime[i] > 0 && !Main.debuff[player.buffType[i]])
                        {
                            player.buffTime[i] = 0;
                            player.buffType[i] = 0;
                        }
                    }
                    break;
                case "drain_mana":
                    player.statMana = 0;
                    break;
                case "set_half_health":
                    player.statLife = player.statLifeMax2 / 2;
                    break;
                case "teleport_random":
                    int tpX = (int)(Main.rand.Next(200, Main.maxTilesX - 200) * 16);
                    int tpY = (int)(Main.rand.Next(50, (int)(Main.worldSurface * 0.35)) * 16);
                    player.Teleport(new Microsoft.Xna.Framework.Vector2(tpX, tpY), 1);
                    break;

                // ==== SPAWNS ====
                case "spawn_slime":
                    SpawnNPC(x + 200, y, Terraria.ID.NPCID.BlueSlime, viewer);
                    break;
                case "spawn_zombie":
                    SpawnNPC(x + 200, y, Terraria.ID.NPCID.Zombie, viewer);
                    break;
                case "spawn_demon_eye":
                    SpawnNPC(x, y - 200, Terraria.ID.NPCID.DemonEye, viewer);
                    break;
                case "spawn_skeleton":
                    SpawnNPC(x + 200, y, Terraria.ID.NPCID.Skeleton, viewer);
                    break;
                case "spawn_pinky":
                    SpawnNPC(x + 150, y, Terraria.ID.NPCID.Pinky, viewer);
                    break;
                case "spawn_mimic":
                    SpawnNPC(x + 200, y, Terraria.ID.NPCID.Mimic, viewer);
                    break;
                case "spawn_nymph":
                    SpawnNPC(x + 200, y, Terraria.ID.NPCID.Nymph, viewer);
                    break;
                case "spawn_bunny":
                    SpawnNPC(x + 100, y, Terraria.ID.NPCID.Bunny, viewer);
                    break;
                case "spawn_goldfish":
                    SpawnNPC(x + 100, y, Terraria.ID.NPCID.Goldfish, viewer);
                    break;

                // ==== BOSSES ====
                case "spawn_king_slime":
                    SpawnNPC(x + 400, y - 400, Terraria.ID.NPCID.KingSlime, viewer);
                    break;
                case "spawn_eye_of_cthulhu":
                    SpawnNPC(x, y - 400, Terraria.ID.NPCID.EyeofCthulhu, viewer);
                    break;
                case "spawn_eater_of_worlds":
                    SpawnNPC(x + 300, y + 200, Terraria.ID.NPCID.EaterofWorldsHead, viewer);
                    break;
                case "spawn_brain_of_cthulhu":
                    SpawnNPC(x, y - 300, Terraria.ID.NPCID.BrainofCthulhu, viewer);
                    break;
                case "spawn_queen_bee":
                    SpawnNPC(x + 300, y - 200, Terraria.ID.NPCID.QueenBee, viewer);
                    break;
                case "spawn_skeletron":
                    SpawnNPC(x, y - 400, Terraria.ID.NPCID.SkeletronHead, viewer);
                    break;
                case "spawn_wall_of_flesh":
                    SpawnNPC(x + 300, y, Terraria.ID.NPCID.WallofFlesh, viewer);
                    break;
                case "spawn_queen_slime":
                    SpawnNPC(x + 400, y - 400, Terraria.ID.NPCID.QueenSlimeBoss, viewer);
                    break;
                case "spawn_twins":
                    SpawnNPC(x, y - 500, Terraria.ID.NPCID.Retinazer, viewer);
                    SpawnNPC(x + 200, y - 500, Terraria.ID.NPCID.Spazmatism, viewer);
                    break;
                case "spawn_destroyer":
                    SpawnNPC(x, y - 500, Terraria.ID.NPCID.TheDestroyer, viewer);
                    break;
                case "spawn_skeletron_prime":
                    SpawnNPC(x, y - 500, Terraria.ID.NPCID.SkeletronPrime, viewer);
                    break;
                case "spawn_plantera":
                    SpawnNPC(x + 300, y, Terraria.ID.NPCID.Plantera, viewer);
                    break;
                case "spawn_golem":
                    SpawnNPC(x + 300, y, Terraria.ID.NPCID.Golem, viewer);
                    break;
                case "spawn_duke_fishron":
                    SpawnNPC(x + 400, y - 300, Terraria.ID.NPCID.DukeFishron, viewer);
                    break;
                case "spawn_empress_of_light":
                    SpawnNPC(x, y - 500, Terraria.ID.NPCID.HallowBoss, viewer);
                    break;
                case "spawn_moon_lord":
                    SpawnNPC(x, y - 600, Terraria.ID.NPCID.MoonLordCore, viewer);
                    break;

                // ==== ENVIRONMENT ====
                case "set_time_day":
                    Main.dayTime = true;
                    Main.time = 0;
                    break;
                case "set_time_night":
                    Main.dayTime = false;
                    Main.time = 0;
                    break;
                case "set_time_noon":
                    Main.dayTime = true;
                    Main.time = 27000;
                    break;
                case "set_time_midnight":
                    Main.dayTime = false;
                    Main.time = 16200;
                    break;
                case "toggle_rain":
                    if (Main.raining) {
                        Main.raining = false;
                        Main.maxRaining = 0f;
                    } else {
                        Main.raining = true;
                        Main.maxRaining = 1f;
                    }
                    break;
                case "start_storm":
                    Main.raining = true;
                    Main.maxRaining = 1f;
                    Main.cloudBGActive = 1f;
                    break;
                case "start_sandstorm":
                    Terraria.GameContent.Events.Sandstorm.StartSandstorm();
                    break;
                case "blood_moon":
                    Main.dayTime = false;
                    Main.time = 0;
                    Main.bloodMoon = true;
                    break;
                case "solar_eclipse":
                    Main.dayTime = true;
                    Main.time = 0;
                    Main.eclipse = true;
                    break;
                case "slime_rain":
                    Main.StartSlimeRain(false);
                    break;
                case "goblin_army":
                    Main.StartInvasion(Terraria.ID.InvasionID.GoblinArmy);
                    break;
                case "pirate_invasion":
                    Main.StartInvasion(Terraria.ID.InvasionID.PirateInvasion);
                    break;

                // ==== ITEMS ====
                case "give_copper_shortsword":
                    player.QuickSpawnItem(player.GetSource_Misc("TrinyTok"), Terraria.ID.ItemID.CopperShortsword, 1);
                    break;
                case "give_wooden_sword":
                    player.QuickSpawnItem(player.GetSource_Misc("TrinyTok"), Terraria.ID.ItemID.WoodenSword, 1);
                    break;
                case "give_nights_edge":
                    player.QuickSpawnItem(player.GetSource_Misc("TrinyTok"), Terraria.ID.ItemID.NightsEdge, 1);
                    break;
                case "give_terra_blade":
                    player.QuickSpawnItem(player.GetSource_Misc("TrinyTok"), Terraria.ID.ItemID.TerraBlade, 1);
                    break;
                case "give_zenith":
                    player.QuickSpawnItem(player.GetSource_Misc("TrinyTok"), Terraria.ID.ItemID.Zenith, 1);
                    break;
                case "give_meowmere":
                    player.QuickSpawnItem(player.GetSource_Misc("TrinyTok"), Terraria.ID.ItemID.Meowmere, 1);
                    break;
                case "give_bomb":
                    player.QuickSpawnItem(player.GetSource_Misc("TrinyTok"), Terraria.ID.ItemID.Bomb, 10);
                    break;
                case "give_dynamite":
                    player.QuickSpawnItem(player.GetSource_Misc("TrinyTok"), Terraria.ID.ItemID.Dynamite, 5);
                    break;
                case "give_healing_potion":
                    player.QuickSpawnItem(player.GetSource_Misc("TrinyTok"), Terraria.ID.ItemID.HealingPotion, 5);
                    break;
                case "give_life_fruit":
                    player.QuickSpawnItem(player.GetSource_Misc("TrinyTok"), Terraria.ID.ItemID.LifeFruit, 1);
                    break;
                case "give_dirt_block":
                    player.QuickSpawnItem(player.GetSource_Misc("TrinyTok"), Terraria.ID.ItemID.DirtBlock, 999);
                    break;
                case "give_angel_wings":
                    player.QuickSpawnItem(player.GetSource_Misc("TrinyTok"), Terraria.ID.ItemID.AngelWings, 1);
                    break;
                case "give_rod_of_discord":
                    player.QuickSpawnItem(player.GetSource_Misc("TrinyTok"), Terraria.ID.ItemID.RodofDiscord, 1);
                    break;
                case "give_terraprisma":
                    player.QuickSpawnItem(player.GetSource_Misc("TrinyTok"), Terraria.ID.ItemID.EmpressBlade, 1);
                    break;

                case "debuff_shimmer":
                    // Sends player phasing through blocks downwards
                    player.AddBuff(Terraria.ID.BuffID.Shimmer, 60 * 5);
                    break;
                case "give_tombstone":
                    player.QuickSpawnItem(player.GetSource_Misc("TrinyTok"), Terraria.ID.ItemID.Tombstone, 1);
                    break;

                default:
                    Main.NewText($"Unknown TrinyTok command: {cmd.code}", 255, 0, 0);
                    break;
            }
        }

        private void SpawnNPC(int x, int y, int npcId, string viewer)
        {
            int index = Terraria.NPC.NewNPC(Main.LocalPlayer.GetSource_Misc("TrinyTok"), x, y, npcId);
            if (index >= 0 && index < Main.maxNPCs)
            {
                Main.npc[index].GivenName = $"{viewer}'s spawn";
            }
        }
    }

    public class CommandPayload
    {
        public int id { get; set; }
        public int type { get; set; }
        public string code { get; set; }
        public string viewer { get; set; }
        public int duration { get; set; }
        public int count { get; set; }
        public int power { get; set; }
    }
}
