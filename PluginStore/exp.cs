using System;
using System.IO;
using System.Collections.Generic;
using MCGalaxy;
using MCGalaxy.Events;
using MCGalaxy.Events.PlayerEvents;
using BlockID = System.UInt16;

public class EXPPlugin : Plugin {
    public override string name { 
        get { return "EXPPlugin"; } 
    }
    public override string MCGalaxy_Version { 
        get { return "1.9.3.0"; } 
    }
    public static EXPPlugin instance;

    // In-memory storage: player name -> EXP and level.
    static Dictionary<string, int> playerEXP = new Dictionary<string, int>();
    static Dictionary<string, int> playerLevel = new Dictionary<string, int>();
    // Store each player's original prefix to preserve their team title and other settings.
    static Dictionary<string, string> originalPrefix = new Dictionary<string, string>();

    // Delegates for events.
    private OnPlayerChat chatDelegate;
    private OnBlockChanging blockChangingDelegate;
    private OnPlayerDied diedDelegate;
    
    private Cmdeexp eexpCmd;

    // Promotion thresholds – if player's level equals any of these, they are promoted.
    static readonly int[] promotionThresholds = new int[] { 10, 50, 100, 150, 200 };

    public override void Load(bool startup) {
        instance = this;
        LoadGlobalData();

        // Register events.
        chatDelegate = new OnPlayerChat(OnPlayerChatHandler);
        OnPlayerChatEvent.Register(chatDelegate, Priority.Low, false);

        blockChangingDelegate = new OnBlockChanging(OnBlockChangingHandler);
        OnBlockChangingEvent.Register(blockChangingDelegate, Priority.Low, false);

        diedDelegate = new OnPlayerDied(OnPlayerDiedHandler);
        OnPlayerDiedEvent.Register(diedDelegate, Priority.Low, false);

        // Register /exp command.
        eexpCmd = new Cmdeexp();
        Command.Register(eexpCmd);
    }

    public override void Unload(bool shutdown) {
        OnPlayerChatEvent.Unregister(chatDelegate);
        OnBlockChangingEvent.Unregister(blockChangingDelegate);
        OnPlayerDiedEvent.Unregister(diedDelegate);
        Command.Unregister(eexpCmd);
        SaveGlobalData();
    }

    // -------------------------
    // Chat Prefix Helper
    // -------------------------
    // Updates the player's prefix to display current level and EXP before their original prefix.
    void UpdateChatPrefix(Player p) {
        // Store the player's original prefix the first time.
        if (!originalPrefix.ContainsKey(p.name)) {
            originalPrefix[p.name] = p.prefix;  // Save current prefix that contains team title, etc.
        }
        p.prefix = "&2Level " + GetLevel(p) + " |%f " + originalPrefix[p.name];
    }

    // -------------------------
    // EXP and Level Persistence
    // -------------------------
    void LoadGlobalData() {
        string folder = "exp";
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);
        string file = Path.Combine(folder, "globalexp.txt");
        if (File.Exists(file)) {
            try {
                string[] lines = File.ReadAllLines(file);
                foreach (string line in lines) {
                    if (!String.IsNullOrWhiteSpace(line)) {
                        // File format: playername exp level
                        string[] parts = line.Split(' ');
                        if (parts.Length >= 3) {
                            string name = parts[0];
                            int expVal, levelVal;
                            int.TryParse(parts[1], out expVal);
                            int.TryParse(parts[2], out levelVal);
                            playerEXP[name] = expVal;
                            playerLevel[name] = levelVal;
                        }
                    }
                }
            } catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }

    void SaveGlobalData() {
        string folder = "exp";
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);
        string file = Path.Combine(folder, "globalexp.txt");
        try {
            List<string> lines = new List<string>();
            foreach (KeyValuePair<string, int> kvp in playerEXP) {
                string name = kvp.Key;
                int expVal = kvp.Value;
                int levelVal = playerLevel.ContainsKey(name) ? playerLevel[name] : 1;
                lines.Add(name + " " + expVal + " " + levelVal);
            }
            File.WriteAllLines(file, lines.ToArray());
        } catch (Exception e) {
            Logger.LogError(e);
        }
    }

    // -------------------------
    // EXP & Level Helpers
    // -------------------------
    // Cumulative EXP thresholds (cumulative system):
    // Level 1 starts at 0 EXP.
    // Level 2 requires 5 EXP total.
    // Level 3 requires 15 EXP total.
    // Level 4 requires 35 EXP total.
    // Level 5 requires 65 EXP total, etc.
    public int CumulativexpForLevel(int level) {
        if (level == 1) return 0;
        if (level == 2) return 5;
        if (level == 3) return 15;
        int cumulative = 15;
        for (int L = 4; L <= level; L++) {
            cumulative += 10 * (L - 2);
        }
        return cumulative;
    }

    public static int GetEXP(Player p) {
        if (!playerEXP.ContainsKey(p.name))
            playerEXP[p.name] = 0;
        return playerEXP[p.name];
    }

    public static void SetEXP(Player p, int exp) {
        playerEXP[p.name] = exp;
    }

    public static int GetLevel(Player p) {
        if (!playerLevel.ContainsKey(p.name))
            playerLevel[p.name] = 1;
        return playerLevel[p.name];
    }

    public static void SetLevel(Player p, int level) {
        playerLevel[p.name] = level;
    }

    // -------------------------
    // Level Up Checking & Promotion
    // -------------------------
    public void CheckLevelUp(Player p) {
        int exp = GetEXP(p);
        int level = GetLevel(p);
        bool leveled = false;
        // Loop to allow multiple level-ups if sufficient EXP is accrued.
        while (exp >= CumulativexpForLevel(level + 1)) {
            level++;
            leveled = true;
            SetLevel(p, level);
            p.Message("&aYou leveled up to level " + level + "!");
            // Automatically promote if level matches any threshold.
            foreach (int thresh in promotionThresholds) {
                if (level == thresh) {
                    Command.Find("promote").Use(Player.Console, p.name);
                    p.Message("&eCongratulations! You have been promoted!");
                    break;
                }
            }
        }
        if (leveled) {
            SaveGlobalData();
            UpdateChatPrefix(p);
        }
    }

    // -------------------------
    // Event Handlers for EXP Gains
    // -------------------------
    // EXP Gains: Chat: +3 EXP, Block placing: +5 EXP, Block breaking: +1 EXP, Dying: +1 EXP.
    void OnPlayerChatHandler(Player p, string message) {
        int exp = GetEXP(p);
        SetEXP(p, exp + 3);
        CheckLevelUp(p);
        UpdateChatPrefix(p);
    }

    void OnBlockChangingHandler(Player p, ushort x, ushort y, ushort z, BlockID block, bool placing, ref bool cancel) {
        int exp = GetEXP(p);
        if (placing)
            SetEXP(p, exp + 5);
        else
            SetEXP(p, exp + 1);
        CheckLevelUp(p);
        UpdateChatPrefix(p);
    }

    void OnPlayerDiedHandler(Player p, BlockID cause, ref TimeSpan cooldown) {
        int exp = GetEXP(p);
        SetEXP(p, exp + 1);
        CheckLevelUp(p);
        UpdateChatPrefix(p);
    }
}

// -------------------------
// Command: /exp
// -------------------------
public class Cmdeexp : Command {
    public override string name { get { return "exp"; } }
    public override string type { get { return CommandTypes.Information; } }

    // /exp shows your EXP, level, and how much EXP is needed for the next level.
    // /exp playername shows the same info for that player.
    // /exp give password player amount lets you give EXP (only for players level 10+)
    public override void Use(Player p, string message) {
        string trimmed = message.Trim();
        if (trimmed == "") {
            DisplayPlayerInfo(p, p);
            return;
        }
        string[] parts = trimmed.Split(' ');
        if (parts.Length >= 1 && parts[0].ToLower() == "give") {
            // expected format: /exp give password player amount
            if (parts.Length != 4) {
                Help(p);
                return;
            }
            if (EXPPlugin.GetLevel(p) < 10) {
                p.Message("&cYou need to be at least level 10 to give EXP.");
                return;
            }
            if (parts[1] != "password") {
                p.Message("&cIncorrect password!");
                return;
            }
            string targetName = parts[2];
            int amount;
            try {
                amount = Convert.ToInt32(parts[3]);
            } catch (Exception) {
                p.Message("&cInvalid amount.");
                return;
            }
            Player target = PlayerInfo.FindMatches(p, targetName);
            if (target == null) {
                p.Message("&cPlayer not found.");
                return;
            }
            int targetEXP = EXPPlugin.GetEXP(target);
            EXPPlugin.SetEXP(target, targetEXP + amount);
            p.Message("&aYou have given " + target.name + " " + amount + " EXP.");
            target.Message("&aYou received " + amount + " EXP from " + p.name + ".");
            EXPPlugin.instance.CheckLevelUp(target);
            return;
        }
        // Otherwise, assume it's a player name lookup.
        Player targetPlayer = PlayerInfo.FindMatches(p, trimmed);
        if (targetPlayer == null) {
            p.Message("&cPlayer not found.");
            return;
        }
        DisplayPlayerInfo(p, targetPlayer);
    }

    // Displays player's EXP, level, and the EXP required for the next level.
    void DisplayPlayerInfo(Player viewer, Player target) {
        int exp = EXPPlugin.GetEXP(target);
        int level = EXPPlugin.GetLevel(target);
        viewer.Message("&a" + target.name + "'s EXP: &e" + exp);
        viewer.Message("&a" + target.name + "'s Level: &e" + level);
        int nextLevelEXP = EXPPlugin.instance.CumulativexpForLevel(level + 1);
        int expNeeded = nextLevelEXP - exp;
        viewer.Message("&aEXP needed for next level: &e" + expNeeded);
    }

    public override void Help(Player p) {
        p.Message("&T/exp");
        p.Message("&HShows your current EXP, level, and how much EXP is needed for the next level.");
        p.Message("&T/exp playername");
        p.Message("&HShows the EXP info of the specified player.");
        p.Message("&T/exp give password player amount");
        p.Message("&HOnly players of level 10+ can use this to give EXP to others.");
    }
}