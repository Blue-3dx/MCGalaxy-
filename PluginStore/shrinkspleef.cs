//reference System.Core.dll

/* NOTE:
    - You need to replace all "SSpleef" strings with the name of your gamemode. E.g "SSpleef", "TNTRun" etc
    - You need to replace all "SSpleef" strings with the name of your gamemode. E.g "SSpleef", "TRUN" etc.
    
    ^ Easiest way is CTRL + H in most text/code editors.
    
    - To add maps, you will need to type /SSpleef add.
    - This is just a template, feel free to modify the config section or add your own behaviour.
*/

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
    public class SSpleefMapConfig
    {
        [ConfigVec3("SSpleef-spawn", null)]
        public Vec3U16 Spawn;

        static string Path(string map) { return "./plugins/SSpleef/maps" + map + ".config"; }
        static ConfigElement[] cfg;

        public void SetDefaults(Level lvl)
        {
            Spawn.X = (ushort)(lvl.Width / 2);
            Spawn.Y = (ushort)(lvl.Height / 2 + 1);
            Spawn.Z = (ushort)(lvl.Length / 2);
        }

        public void Load(string map)
        {
            if (cfg == null) cfg = ConfigElement.GetAll(typeof(SSpleefMapConfig));
            ConfigElement.ParseFile(cfg, Path(map), this);
        }

        public void Save(string map)
        {
            if (cfg == null) cfg = ConfigElement.GetAll(typeof(SSpleefMapConfig));
            // Open a StreamWriter for the given file path so that the second parameter is a StreamWriter
            using (StreamWriter writer = new StreamWriter(Path(map)))
            {
                ConfigElement.Serialise(cfg, writer, this);
            }
        }
    }


    public sealed class SSpleefData
    {
        public int Tokens = 0; // Tokens earned throughout the round
        public int Kills = 0; // Total kills
    }

    public sealed class SSpleefPlugin : Plugin
    {
        public override string creator { get { return "Venk"; } }
        public override string MCGalaxy_Version { get { return "1.9.4.6"; } }
        public override string name { get { return "SSpleef"; } }

        public static ChatToken SSpleefToken;

        static string TokenSSpleef(Player p)
        {
            Player[] players = PlayerInfo.Online.Items;
            int count = 0;

            foreach (Player pl in players)
            {
                if (!SSpleefGame.Instance.Running) return "0";
                if (pl.level.name == SSpleefGame.Instance.Map.name) count++;
            }

            return count.ToString();
        }

        // Table structure for custom statistics
        ColumnDesc[] createDatabase = new ColumnDesc[] {
            new ColumnDesc("Name", ColumnType.VarChar, 16),
            new ColumnDesc("RoundsPlayed", ColumnType.Int32),
            new ColumnDesc("RoundSSpleefon", ColumnType.Int32),
            new ColumnDesc("MoneyEarned", ColumnType.Int32),
            new ColumnDesc("Kills", ColumnType.Int32), // You need to add support for this yourself
            // Add any other columns here
        };

        public override void Load(bool startup)
        {
            // Add token into the server
            SSpleefToken = new ChatToken("$SSpleef", "SSpleef", TokenSSpleef);
            ChatTokens.Standard.Add(SSpleefToken);

            OnConfigUpdatedEvent.Register(OnConfigUpdated, Priority.Low);

            SSpleefGame.Instance.Config.Path = "plugins/SSpleef/game.properties";
            OnConfigUpdated();

            if (SSpleefGame.customStats) Database.CreateTable("Stats_SSpleef", createDatabase); // Initialize database for custom stats

            Command.Register(new CmdSSpleef());

            RoundsGame game = SSpleefGame.Instance;
            game.GetConfig().Load();
            if (!game.Running) game.AutoStart();
        }

        public override void Unload(bool shutdown)
        {
            ChatTokens.Standard.Remove(SSpleefToken);

            OnConfigUpdatedEvent.Unregister(OnConfigUpdated);

            Command.Unregister(Command.Find("SSpleef"));

            RoundsGame game = SSpleefGame.Instance;
            if (game.Running) game.End();
        }

        void OnConfigUpdated()
        {
            SSpleefGame.Instance.Config.Load();
        }
    }

    public sealed class SSpleefConfig : RoundsGameConfig
    {
        public override bool AllowAutoload { get { return true; } }
        protected override string GameName { get { return "SSpleef"; } }
    }

    public sealed partial class SSpleefGame : RoundsGame
    {
        public VolatileArray<Player> Alive = new VolatileArray<Player>();

        public static SSpleefGame Instance = new SSpleefGame();
        public SSpleefGame() { Picker = new SimpleLevelPicker(); }

        public SSpleefConfig Config = new SSpleefConfig();
        public override RoundsGameConfig GetConfig() { return Config; }

        public override string GameName { get { return "SSpleef"; } }
        public int Interval = 1000;
        public SSpleefMapConfig cfg = new SSpleefMapConfig();

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
            cfg = new SSpleefMapConfig();
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
            if (p.level != SSpleefGame.Instance.Map) return;
            if (Picker.HandlesMessage(p, message)) { p.cancelchat = true; return; }
        }

        // This event is called when a player is killed
        void HandlePlayerSpawning(Player p, ref Position pos, ref byte yaw, ref byte pitch, bool respawning)
        {
            if (!respawning || !Alive.Contains(p)) return;
            if (p.Game.Referee) return;
            if (p.level != Map) return;

            Alive.Remove(p); // Remove them from the alive list
            UpdatePlayersLeft();
            p.Game.Referee = false; // This allows them to fly and noclip when they die
            p.Send(Packet.HackControl(true, true, true, true, true, -1)); // ^

            Entities.GlobalDespawn(p, true); // Remove from tab list
            Server.hidden.Add(p.name);
        }

        // We use this event for resetting everything and preparing for the next map
        void HandleOnJoinedLevel(Player p, Level prevLevel, Level level, ref bool announce)
        {
            p.Extras.Remove("SSpleef_INDEX");
            HandleJoinedCommon(p, prevLevel, level, ref announce);

            Entities.GlobalSpawn(p, true); // Adds player back to the tab list

            if (level == Map)
            {
                // Revert back to -hax
                p.Game.Referee = false;
                p.Send(Packet.Motd(p, "-hax -push +thirdperson -inventory"));
                p.invincible = true;

                if (Running)
                {
                    if (RoundInProgress)
                    {
                        // Force spectator mode if they join late
                        p.Game.Referee = true;
                        p.Send(Packet.HackControl(true, true, true, true, true, -1));
                        p.Message("You joined in the middle of the round so you are now a spectator.");
                        return;
                    }

                    else
                    {
                        List<Player> players = level.getPlayers();

                        foreach (Player pl in players)
                        {
                            Server.hidden.Remove(pl.name);
                            pl.Extras.Remove("SSpleef_INDEX");
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

        const string SSpleefExtrasKey = "MCG_SSpleef_DATA";
        public static SSpleefData Get(Player p)
        {
            SSpleefData data = TryGet(p);
            if (data != null) return data;
            data = new SSpleefData();

            p.Extras[SSpleefExtrasKey] = data;
            return data;
        }

        static SSpleefData TryGet(Player p)
        {
            object data; p.Extras.TryGet(SSpleefExtrasKey, out data); return (SSpleefData)data;
        }

        // ============================================ ROUND =======================================
        int roundsOnThisMap = 1;

        protected override void DoRound()
        {
            if (!Running) return;
            SSpleefGame.Instance.Map.Config.Deletable = false;
            SSpleefGame.Instance.Map.Config.Buildable = false;
            Map.UpdateBlockPermissions();

            DoRoundCountdown(countdownTimer); // Countdown to check for players before starting round
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
                    pl.Extras.Remove("SSpleef_INDEX");

                    pl.invincible = false;

                    pl.Send(Packet.Motd(pl, "-hax -push +thirdperson -inventory"));
                    pl.Extras["MOTD"] = "-hax -push +thirdperson -inventory";

                    if (SSpleefGame.customStats)
                    {
                        // Custom statistics
                        List<string[]> rows = Database.GetRows("Stats_SSpleef", "*", "WHERE Name=@0", pl.truename);

                        if (rows.Count == 0)
                        {
                            Database.AddRow("Stats_SSpleef", "Name, RoundsPlayed, RoundSSpleefon, MoneyEarned, Kills", pl.truename, 1, 0, 0, 0);
                        }

                        else
                        {
                            int played = int.Parse(rows[0][1]);
                            Database.UpdateRows("Stats_SSpleef", "RoundsPlayed=@1", "WHERE NAME=@0", pl.truename, played + 1);
                        }
                    }
                }
            }

            // Allow modifying of the map

            if (buildable) SSpleefGame.Instance.Map.Config.Buildable = true;
            if (deletable) SSpleefGame.Instance.Map.Config.Deletable = true;
            Map.UpdateBlockPermissions();

            UpdateAllStatus1();

            // --- Begin added map shrink mechanic ---
            // Remove outer blocks every 500ms (except air (0), water (8,9) and lava (10,11))
            int shrinkOffset = 0;
            while (RoundInProgress && Alive.Count > 0)
            {
                Thread.Sleep(3000);

                Level map = Map;
                int width = map.Width;
                int height = map.Height;
                int length = map.Length;

                shrinkOffset++;

                int minX = shrinkOffset;
                int maxX = width - shrinkOffset - 1;
                int minZ = shrinkOffset;
                int maxZ = length - shrinkOffset - 1;

                for (int x = minX; x <= maxX; x++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        if (x == minX || x == maxX || z == minZ || z == maxZ)
                        {
                            for (int y = 0; y < height; y++)
                            {
                                BlockID block = map.GetBlock((ushort)x, (ushort)y, (ushort)z);
                                if (block != 0 && block != 8 && block != 9 && block != 10 && block != 11)
                                {
                                    map.Blockchange((ushort)x, (ushort)y, (ushort)z, 0);
                                }
                            }
                        }
                    }
                }
            }
            // --- End added map shrink mechanic ---
        }

        void UpdatePlayersLeft()
        {
            if (!RoundInProgress) return;
            Player[] alive = Alive.Items;
            List<Player> players = Map.getPlayers();

            if (alive.Length == 1)
            {
                // Prevent players from fighting after round ends
                foreach (Player pl in players) pl.Extras["PVP_CAN_KILL"] = true;

                // Nobody left except winner
                Map.Message(alive[0].ColoredName + " %Sis the winner!");
                Command.Find("announce").Use(null, "global " + alive[0].ColoredName + " %2Is The Winner");

                SSpleefGame.Instance.Map.Config.Deletable = false;
                SSpleefGame.Instance.Map.Config.Buildable = false;
                Map.UpdateBlockPermissions();

                EndRound(alive[0]);
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
                    List<string[]> rows = Database.GetRows("Stats_SSpleef", "*", "WHERE Name=@0", winner.truename);

                    if (rows.Count == 0)
                    {
                        Database.AddRow("Stats_SSpleef", "Name, RoundsPlayed, RoundSSpleefon, MoneyEarned, Kills", winner.truename, 1, 1, 0, 0);
                    }

                    else
                    {
                        int wins = int.Parse(rows[0][2]);
                        Database.UpdateRows("Stats_SSpleef", "RoundSSpleefon=@1", "WHERE NAME=@0", winner.truename, wins + 1);
                    }
                }

                SSpleefData data = Get(pl);

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
                    winner.Message("%dCongratulations, you won this round of SSpleef!");
                    data.Tokens += winReward;
                }

                if (customStats)
                {
                    // Custom statistics
                    List<string[]> rows = Database.GetRows("Stats_SSpleef", "*", "WHERE Name=@0", pl.truename);

                    if (rows.Count == 0)
                    {
                        Database.AddRow("Stats_SSpleef", "Name, RoundsPlayed, RoundSSpleefon, MoneyEarned, Kills", pl.truename, 0, 0, data.Tokens, 0);
                    }

                    else
                    {
                        int winnings = int.Parse(rows[0][3]);
                        Database.UpdateRows("Stats_SSpleef", "MoneyEarned=@1", "WHERE NAME=@0", pl.truename, winnings + data.Tokens);
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

    // This is the command the player will type. E.g, /SSpleef or /SSpleef
    public sealed class CmdSSpleef : RoundsGameCmd
    {
        public override string name { get { return "SSpleef"; } }
        public override string shortcut { get { return "SSpleef"; } }
        protected override RoundsGame Game { get { return SSpleefGame.Instance; } }
        public override CommandPerm[] ExtraPerms
        {
            get { return new[] { new CommandPerm(LevelPermission.Operator, "can manage SSpleef") }; }
        }

        protected override void HandleStart(Player p, RoundsGame game, string[] args)
        {
            if (game.Running) { p.Message("{0} is already running", game.GameName); return; }

            int interval = 150;
            if (args.Length > 1 && !CommandParser.GetInt(p, args[1], "Delay", ref interval, 1, 1000)) return;

            ((SSpleefGame)game).Interval = interval;
            game.Start(p, "", int.MaxValue);
        }

        protected override void HandleSet(Player p, RoundsGame game, string[] args)
        {
            if (args.Length < 2) { Help(p, "set"); return; }
            string prop = args[1];

            if (prop.CaselessEq("spawn"))
            {
                SSpleefMapConfig cfg = RetrieveConfig(p);
                cfg.Spawn = (Vec3U16)p.Pos.FeetBlockCoords;
                p.Message("Set spawn pos to: &b{0}", cfg.Spawn);
                UpdateConfig(p, cfg);
                return;
            }

            if (args.Length < 3) { Help(p, "set"); }
        }

        static SSpleefMapConfig RetrieveConfig(Player p)
        {
            SSpleefMapConfig cfg = new SSpleefMapConfig();
            cfg.SetDefaults(p.level);
            cfg.Load(p.level.name);
            return cfg;
        }

        static void UpdateConfig(Player p, SSpleefMapConfig cfg)
        {
            if (!Directory.Exists("SSpleef")) Directory.CreateDirectory("SSpleef");
            cfg.Save(p.level.name);

            if (p.level == SSpleefGame.Instance.Map)
                SSpleefGame.Instance.UpdateMapConfig();
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
            p.Message("%T/SSpleef start %H- Starts a game of SSpleef");
            p.Message("%T/SSpleef stop %H- Immediately stops SSpleef");
            p.Message("%T/SSpleef end %H- Ends current round of SSpleef");
            p.Message("%T/SSpleef add/remove %H- Adds/removes current map from the map list");
            p.Message("%T/SSpleef status %H- Outputs current status of SSpleef");
            p.Message("%T/SSpleef go %H- Moves you to the current SSpleef map.");
        }
    }
}
