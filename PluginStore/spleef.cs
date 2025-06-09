//reference System.Core.dll
//pluginref NewLevelPicker.dll
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

using MCGalaxy.Commands;
using MCGalaxy.Commands.Fun;
using MCGalaxy.Config;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Events.ServerEvents;
using MCGalaxy.Maths;
using MCGalaxy.Network;
using MCGalaxy.SQL;

using BlockID = System.UInt16;

namespace MCGalaxy.Games
{
    public class SpleefMapConfig
    {
        [ConfigVec3("Spleef-spawn", null)]
        public Vec3U16 Spawn;

        static string Path(string map) { return "./plugins/Spleef/maps" + map + ".config"; }
        static ConfigElement[] cfg;

        public void SetDefaults(Level lvl)
        {
            Spawn.X = (ushort)(lvl.Width / 2);
            Spawn.Y = (ushort)(lvl.Height / 2 + 1);
            Spawn.Z = (ushort)(lvl.Length / 2);
        }

        public void Load(string map)
        {
            if (cfg == null) cfg = ConfigElement.GetAll(typeof(SpleefMapConfig));
            ConfigElement.ParseFile(cfg, Path(map), this);
        }

        public void Save(string map)
        {
            if (cfg == null) cfg = ConfigElement.GetAll(typeof(SpleefMapConfig));
            ConfigElement.SerialiseSimple(cfg, Path(map), this);
        }
    }


    public sealed class SpleefData
    {
        public int Tokens = 0; // Tokens earned throughout the round
        public int Kills = 0; // Total kills
    }

    public sealed class SpleefPlugin : Plugin
    {
        public override string creator { get { return "Blue_3dx"; } }
        public override string MCGalaxy_Version { get { return "1.9.4.9"; } }
        public override string name { get { return "Spleef"; } }

        public static ChatToken SpleefToken;

        static string TokenSpleef(Player p)
        {
            Player[] players = PlayerInfo.Online.Items;
            int count = 0;

            foreach (Player pl in players)
            {
                if (!SpleefGame.Instance.Running) return "0";
                if (pl.level.name == SpleefGame.Instance.Map.name) count++;
            }

            return count.ToString();
        }

        // Table structure for custom statistics
        ColumnDesc[] createDatabase = new ColumnDesc[] {
            new ColumnDesc("Name", ColumnType.VarChar, 16),
            new ColumnDesc("RoundsPlayed", ColumnType.Int32),
            new ColumnDesc("RoundSpleefon", ColumnType.Int32),
            new ColumnDesc("MoneyEarned", ColumnType.Int32),
            new ColumnDesc("Kills", ColumnType.Int32), // You need to add support for this yourself
            // Add any other columns here
        };

        public override void Load(bool startup)
        {
            // Add token into the server
            SpleefToken = new ChatToken("$Spleef", "Spleef", TokenSpleef);
            ChatTokens.Standard.Add(SpleefToken);

            OnConfigUpdatedEvent.Register(OnConfigUpdated, Priority.Low);

            SpleefGame.Instance.Config.Path = "plugins/Spleef/game.properties";
            OnConfigUpdated();

            if (SpleefGame.customStats) Database.CreateTable("Stats_Spleef", createDatabase); // Initialize database for custom stats

            Command.Register(new CmdSpleef());
            Command.Register(new CmdTS());

            RoundsGame game = SpleefGame.Instance;
            game.GetConfig().Load();
            if (!game.Running) game.AutoStart();
        }

        public override void Unload(bool shutdown)
        {
            ChatTokens.Standard.Remove(SpleefToken);

            OnConfigUpdatedEvent.Unregister(OnConfigUpdated);

            Command.Unregister(Command.Find("Spleef"));
            Command.Unregister(Command.Find("TS"));

            RoundsGame game = SpleefGame.Instance;
            if (game.Running) game.End();
        }

        void OnConfigUpdated()
        {
            SpleefGame.Instance.Config.Load();
        }
    }

    public sealed class SpleefConfig : RoundsGameConfig
    {
        public override bool AllowAutoload { get { return true; } }
        protected override string GameName { get { return "Spleef"; } }
    }

    public sealed partial class SpleefGame : RoundsGame
    {
        public VolatileArray<Player> Alive = new VolatileArray<Player>();

        public static SpleefGame Instance = new SpleefGame();
public List<string> TrueSpleeferChallengers = new List<string>();
        public SpleefGame() { Picker = new NewerLevelPicker(); }

        public SpleefConfig Config = new SpleefConfig();
        public override RoundsGameConfig GetConfig() { return Config; }

        public override string GameName { get { return "Spleef"; } }
        public int Interval = 1000;
        public SpleefMapConfig cfg = new SpleefMapConfig();

        protected override string WelcomeMessage
        {
            get { return ""; } // Message shown to players when connecting
        }

        // =========================================== CONFIG =======================================

        public static bool pvp = false; // (Requires VenksSurvival plugin) Whether or not to allow players to fight each other
        public static bool buildable = false; // Whether or not to make the map buildable on round start
        public static bool deletable = true; // Whether or not to make the map deletable on round start
        public static bool altDetection = false; // Whether or not to give rewards to players if they share an IP with any players online
        public static bool customStats = true; // Whether or not the plugin should implement custom statistics for rounds played, wins and money earned

        public static int winReward = 50; // Amount given to the player who wins
        public static int killReward = 10; // Amount given to players for every kill (incremental)
        public static int participationReward = 5; // Amount given to players for playing a round
        public static int countdownTimer = 10; // Time (in seconds) to check for players before starting a round

        // ============================================ GAME =======================================
        public override void UpdateMapConfig()
        {
            cfg = new SpleefMapConfig();
            cfg.SetDefaults(Map);
            cfg.Load(Map.name);
        }

        protected override List<Player> GetPlayers()
        {
            return Map.getPlayers();
        }

        public override void OutputStatus(Player p)
        {
            Player[] alive = Alive.Items;
            p.Message("Alive players: " + alive.Join(pl => pl.ColoredName));
        }

        public override void Start(Player p, string map, int rounds)
        {
            // Starts on current map by default
            if (!p.IsSuper && map.Length == 0) map = p.level.name;
            base.Start(p, map, rounds);
        }

        protected override void StartGame() { Config.Load(); }

        protected override void EndGame()
        {
            if (RoundInProgress) EndRound(null);
            Alive.Clear();
        }

        public override void PlayerLeftGame(Player p)
        {
            p.Extras.Remove("SURVIVAL_HIDE_HUD");
            // "kill" player if they leave server or change map
            if (!Alive.Contains(p)) return;
            Alive.Remove(p);

            // ===== NEW SNIPPET START: Prevent dead players from building =====
            if (p.Session.Supports("BlockPermissions", 1))
            {
                bool extBlocks = p.Session.hasExtBlocks;
                byte[] data = new byte[extBlocks ? 5 : 4];
    
                Packet.WriteBlockPermission(p.Session.ConvertBlock(Block.Air), false, false, extBlocks, data, 0);
                p.Session.Send(data);
            }
            // ===== NEW SNIPPET END =====

            UpdatePlayersLeft();
        }

        protected override string FormatStatus1(Player p)
        {
            return RoundInProgress ? "%b" + Alive.Count + " %Splayers left" : "";
        }

        // ============================================ PLUGIN =======================================		
        protected override void HookEventHandlers()
        {
            OnPlayerSpawningEvent.Register(HandlePlayerSpawning, Priority.High);
            OnJoinedLevelEvent.Register(HandleOnJoinedLevel, Priority.High);
            OnPlayerChatEvent.Register(HandlePlayerChat, Priority.High);

            base.HookEventHandlers();
        }

        protected override void UnhookEventHandlers()
        {
            OnPlayerSpawningEvent.Unregister(HandlePlayerSpawning);
            OnJoinedLevelEvent.Unregister(HandleOnJoinedLevel);
            OnPlayerChatEvent.Unregister(HandlePlayerChat);

            base.UnhookEventHandlers();
        }

        // Checks if player votes for a map when voting in progress "1, 2, 3"
        void HandlePlayerChat(Player p, string message)
        {
            if (p.level != SpleefGame.Instance.Map) return;
            if (Picker.HandlesMessage(p, message)) { p.cancelchat = true; return; }
        }

        // This event is called when a player is killed
        void HandlePlayerSpawning(Player p, ref Position pos, ref byte yaw, ref byte pitch, bool respawning)
        {
            if (!respawning || !Alive.Contains(p)) return;
            if (p.Game.Referee) return;
            if (p.level != Map) return;
            // fixing attempt number 1
            
            Alive.Remove(p);			// Remove them from the alive list
// === True Spleefer: failure ===
if (SpleefGame.Instance.TrueSpleeferChallengers.Contains(p.name)) {
    Command.Find("say").Use(p, p.name + " %CFAILED The %2True Spleefer Challenge!");
    Command.Find("take").Use(p, p.name + " 30");
    

}

// === end failure ===


            // ===== NEW SNIPPET START: Prevent dead players from building =====
            if (p.Session.Supports("BlockPermissions", 1))
            {
                p.AllowBuild = false;
            }
            // ===== NEW SNIPPET END =====
            UpdatePlayersLeft();
            p.Game.Referee = false; // This allows them to fly and noclip when they die
            p.Send(Packet.HackControl(true, true, true, true, true, -1)); // ^
        
            Server.hidden.Add(p.name);
        }

        // We use this event for resetting everything and preparing for the next map
        void HandleOnJoinedLevel(Player p, Level prevLevel, Level level, ref bool announce)
        {
            p.Extras.Remove("Spleef_INDEX");
            HandleJoinedCommon(p, prevLevel, level, ref announce);

            Entities.GlobalSpawn(p, true); // Adds player back to the tab list

            if (level == Map)
            {
                // Revert back to -hax
                p.Game.Referee = false;
                p.Send(Packet.Motd(p, "+hold -hax -push +thirdperson -inventory"));
                p.invincible = true;

                if (Running)
                {
                    if (RoundInProgress)
                    {
                        // Force spectator mode if they join late
                        p.Game.Referee = true;
                        p.Send(Packet.HackControl(true, true, true, true, true, -1));
                        p.Message("You joined in the middle of the round so you are now a spectator.");
                        if (p.Session.Supports("BlockPermissions", 1))
                          {
                                 p.AllowBuild = false;
                          }
                        return;
                    }
                    else
                    {
                        List<Player> players = level.getPlayers();

                        foreach (Player pl in players)
                        {
                            Server.hidden.Remove(pl.name);
                            pl.Extras.Remove("Spleef_INDEX");
                        }
                    }
                }
            }
            else
            {
                p.Game.Referee = false;
                p.invincible = false;
            }
        }

        const string SpleefExtrasKey = "MCG_Spleef_DATA";
        public static SpleefData Get(Player p)
        {
            SpleefData data = TryGet(p);
            if (data != null) return data;
            data = new SpleefData();

            p.Extras[SpleefExtrasKey] = data;
            return data;
        }

        static SpleefData TryGet(Player p)
        {
            object data; p.Extras.TryGet(SpleefExtrasKey, out data); return (SpleefData)data;
        }

        // ============================================ ROUND =======================================
        int roundsOnThisMap = 1;

        protected override void DoRound()
        {
            if (!Running) return;
            SpleefGame.Instance.Map.Config.Deletable = false;
            SpleefGame.Instance.Map.Config.Buildable = false;
            Map.UpdateBlockPermissions();

            DoRoundCountdown(countdownTimer); // Countdown to check if there are enough players before starting round
            if (!Running) return;

            UpdateMapConfig();
            if (!Running) return;

            List<Player> players = Map.getPlayers();

            foreach (Player pl in players)
            {
                Alive.Add(pl); // Adds them to the alive list
            }

            if (!Running) return;

            RoundInProgress = true;

            foreach (Player pl in players)
            {
                if (pl.level == Map)
                {
                    pl.Extras.Remove("SURVIVAL_HIDE_HUD");

                    if (pl.Game.Referee) continue;

                    Alive.Add(pl);

                    if (pvp) pl.Extras["PVP_CAN_KILL"] = true;
                    pl.Extras.Remove("Spleef_INDEX");

                    pl.invincible = false;

                    pl.Send(Packet.Motd(pl, "+hold -hax -push +thirdperson -inventory"));
                    pl.Extras["MOTD"] = "+hold -hax -push +thirdperson -inventory";

                    if (SpleefGame.customStats)
                    {
                        // Custom statistics
                        List<string[]> rows = Database.GetRows("Stats_Spleef", "*", "WHERE Name=@0", pl.truename);

                        if (rows.Count == 0)
                        {
                            Database.AddRow("Stats_Spleef", "Name, RoundsPlayed, RoundSpleefon, MoneyEarned, Kills", pl.truename, 1, 0, 0, 0);
                        }
                        else
                        {
                            int played = int.Parse(rows[0][1]);
                            Database.UpdateRows("Stats_Spleef", "RoundsPlayed=@1", "WHERE NAME=@0", pl.truename, played + 1);
                        }
                    }
                }
            }

            // Allow modifying of the map
            if (buildable) SpleefGame.Instance.Map.Config.Buildable = true;
            if (deletable) SpleefGame.Instance.Map.Config.Deletable = true;
            foreach(Player p in PlayerInfo.Online.Items) {
            if (p.level != Map) return;
            if (p.Session.Supports("BlockPermissions", 1))
            {
                p.AllowBuild = true;
            }

            }
            Map.UpdateBlockPermissions();

            UpdateAllStatus1();

            while (RoundInProgress && Alive.Count > 0)
            {
                Thread.Sleep(Interval);

                Level map = Map;
            }
        }

        void UpdatePlayersLeft()
        {
            if (!RoundInProgress) return;
            Player[] alive = Alive.Items;
            List<Player> players = Map.getPlayers();

if (alive.Length == 1)
{
    Player winner = alive[0];

    // Show winner message
    Map.Message("%r " + winner.name + " %Sis the winner!");

    // End the round properly (handles all cleanup and rewards)
    EndRound(winner);
}

            else
            {
                // Show alive player count
                Map.Message("%b" + alive.Length + " %Splayers left!");
            }
            UpdateAllStatus1();
        }


public override void EndRound() { EndRound(null); }
void EndRound(Player winner)
{
    // === True Spleefer: success (only for challengers) ===
    if (winner != null && SpleefGame.Instance.TrueSpleeferChallengers.Contains(winner.name))
    {
        Command.Find("say").Use(winner, winner.name + " %2Completed The %cTrue Spleefer Challenge%2!");
        Command.Find("give").Use(winner, winner.name + " 60");
    }
    // Clear challengers for next round
    SpleefGame.Instance.TrueSpleeferChallengers.Clear();

    // Reset round state
    RoundInProgress = false;
    Alive.Clear();

    // Temporary IP storage for alt detection
    List<string> uniqueIPs = new List<string>();

    Player[] players = PlayerInfo.Online.Items;
    foreach (Player pl in players)
    {
        if (pl.level != Instance.Map) continue;
        pl.Extras["SURVIVAL_HIDE_HUD"] = true;

        if (customStats && pl == winner)
        {
                    // Custom statistics
                    List<string[]> rows = Database.GetRows("Stats_Spleef", "*", "WHERE Name=@0", winner.truename);

                    if (rows.Count == 0)
                    {
                        Database.AddRow("Stats_Spleef", "Name, RoundsPlayed, RoundSpleefon, MoneyEarned, Kills", winner.truename, 1, 1, 0, 0);
                    }
                    else
                    {
                        int wins = int.Parse(rows[0][2]);
                        Database.UpdateRows("Stats_Spleef", "RoundSpleefon=@1", "WHERE NAME=@0", winner.truename, wins + 1);
                    }
                }

                SpleefData data = Get(pl);

                if (altDetection)
                {
                    if (uniqueIPs.Contains(pl.ip))
                    {
                        pl.Message("%7You have been detected as playing with an alt. As such, you have not earned any tokens this round.");
                        continue;
                    }

                    uniqueIPs.Add(pl.ip);
                }

                if (participationReward > 0) data.Tokens += participationReward;

                if (killReward > 0)
                {
                    if (data.Kills > 0)
                    {
                        data.Tokens += data.Kills * killReward;
                        pl.Message(data.Kills + " %7kills = %b" + data.Kills + " %f↕");
                    }
                }

                if (pl == winner)
                {
                    winner.Message("%dCongratulations, you won this round of Spleef!");
                    data.Tokens += winReward;
                }

                if (customStats)
                {
                    // Custom statistics
                    List<string[]> rows = Database.GetRows("Stats_Spleef", "*", "WHERE Name=@0", pl.truename);

                    if (rows.Count == 0)
                    {
                        Database.AddRow("Stats_Spleef", "Name, RoundsPlayed, RoundSpleefon, MoneyEarned, Kills", pl.truename, 0, 0, data.Tokens, 0);
                    }
                    else
                    {
                        int winnings = int.Parse(rows[0][3]);
                        Database.UpdateRows("Stats_Spleef", "MoneyEarned=@1", "WHERE NAME=@0", pl.truename, winnings + data.Tokens);
                    }
                }

                pl.SetMoney(pl.money + data.Tokens);
            }

            if (altDetection) uniqueIPs.Clear();

            UpdateAllStatus1();

            BufferedBlockSender bulk = new BufferedBlockSender(Map);

            bulk.Flush();
}

        // ============================================ STATS =======================================
    }

    // This is the command the player will type. E.g, /Spleef or /Spleef
    // ───────────────────────────────────────────────────────────────
    // Your existing CmdSpleef class (unchanged)
    // ───────────────────────────────────────────────────────────────

    public sealed class CmdSpleef : RoundsGameCmd
    {
        public override string name { get { return "Spleef"; } }
        public override string shortcut { get { return "Spleef"; } }
        protected override RoundsGame Game { get { return SpleefGame.Instance; } }
        public override CommandPerm[] ExtraPerms
        {
            get { return new[] { new CommandPerm(LevelPermission.Operator, "can manage Spleef") }; }
        }

        protected override void HandleStart(Player p, RoundsGame game, string[] args)
        {
            if (game.Running) { p.Message("{0} is already running", game.GameName); return; }

            int interval = 150;
            if (args.Length > 1 && !CommandParser.GetInt(p, args[1], "Delay", ref interval, 1, 1000)) return;

            ((SpleefGame)game).Interval = interval;
            game.Start(p, "", int.MaxValue);
        }

        protected override void HandleSet(Player p, RoundsGame game, string[] args)
        {
            if (args.Length < 2) { Help(p, "set"); return; }
            string prop = args[1];

            if (prop.CaselessEq("spawn"))
            {
                SpleefMapConfig cfg = RetrieveConfig(p);
                cfg.Spawn = (Vec3U16)p.Pos.FeetBlockCoords;
                p.Message("Set spawn pos to: &b{0}", cfg.Spawn);
                UpdateConfig(p, cfg);
                return;
            }

            if (args.Length < 3) { Help(p, "set"); }
        }

        static SpleefMapConfig RetrieveConfig(Player p)
        {
            SpleefMapConfig cfg = new SpleefMapConfig();
            cfg.SetDefaults(p.level);
            cfg.Load(p.level.name);
            return cfg;
        }

        static void UpdateConfig(Player p, SpleefMapConfig cfg)
        {
            if (!Directory.Exists("Spleef")) Directory.CreateDirectory("Spleef");
            cfg.Save(p.level.name);

            if (p.level == SpleefGame.Instance.Map)
                SpleefGame.Instance.UpdateMapConfig();
        }

        public override void Help(Player p, string message)
        {
            if (message.CaselessEq("h2p"))
            {
                p.Message("%H2-16 players will spawn. You will have 10 seconds grace");
                p.Message("%Hperiod in which you cannot be killed. After these");
                p.Message("%H10 seconds it's anyone's game. Click on chests to gain");
                p.Message("%Hloot and click on people to attack them.");
                p.Message("%HLast person standing wins the game.");
            }
            else
            {
                base.Help(p, message);
            }
        }

        public override void Help(Player p)
        {
            p.Message("%T/Spleef start %H- Starts a game of Spleef");
            p.Message("%T/Spleef stop %H- Immediately stops Spleef");
            p.Message("%T/Spleef end %H- Ends current round of Spleef");
            p.Message("%T/Spleef add/remove %H- Adds/removes current map from the map list");
            p.Message("%T/Spleef status %H- Outputs current status of Spleef");
            p.Message("%T/Spleef go %H- Moves you to the current Spleef map.");
        }
    }

    // ───────────────────────────────────────────────────────────────
    // True Spleefer Challenge command
    // ───────────────────────────────────────────────────────────────
public sealed class CmdTS : Command {
    public override string name        { get { return "ts"; } }
    public override string shortcut    { get { return "TS"; } }
    public override string type        { get { return CommandTypes.Games; } }
    public override LevelPermission defaultRank { get { return LevelPermission.Guest; } }

    public override void Use(Player p, string message) {
        // Store the challenger
        if (!SpleefGame.Instance.TrueSpleeferChallengers.Contains(p.name)) {
            SpleefGame.Instance.TrueSpleeferChallengers.Add(p.name);
            foreach (Player d in PlayerInfo.Online.Items) {
                d.Message(p.name + " %2Took The %cTrue Spleefer Challenge%2!");
            }
        } else {
            p.Message("You already took the True Spleefer Challenge.");
            return;
        }
    }

    public override void Help(Player p) {
        p.Message("/ts - Begin the True Spleefer Challenge");
    }
}  // closes CmdTS


}  // closes namespace MCGalaxy.Games
