using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using MCGalaxy;
using MCGalaxy.Events.PlayerEvents;

public sealed class AntiCheatClient : Plugin
{
    public override string name { get { return "AntiCheatClient"; } }
    public override string creator { get { return "Blue_3dx"; } }
    public override string MCGalaxy_Version { get { return "1.9.4.9"; } }

    static bool antiCheatEnabled = false;
    static CmdAntiCheat cmd = new CmdAntiCheat();

    // List of cheat client names loaded from the file.
    static List<string> cheatClients = new List<string>();
    // List of trusted player names (lowercase) loaded from file.
    static List<string> trustedPlayers = new List<string>();

    public override void Load(bool startup)
    {
        LoadCheatClientList();
        LoadTrustedList();
        OnPlayerConnectEvent.Register(OnPlayerConnect, Priority.High);
        Command.Register(cmd);
    }

    public override void Unload(bool shutdown)
    {
        OnPlayerConnectEvent.Unregister(OnPlayerConnect);
        Command.Unregister(cmd);
    }

    /// <summary>
    /// Loads the cheat client names from ANTICHEAT/CheatClients.txt.
    /// If the file doesn't exist, it creates one with a sample entry.
    /// </summary>
    void LoadCheatClientList()
    {
        string folder = "ANTICHEAT";
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        string filePath = Path.Combine(folder, "CheatClients.txt");
        if (!File.Exists(filePath))
        {
            // Create the file with a sample cheat client name.
            File.WriteAllText(filePath, "ExampleCheatClient");
        }

        cheatClients.Clear();
        foreach (string line in File.ReadAllLines(filePath))
        {
            string trimmed = line.Trim();
            if (trimmed.Length > 0)
                cheatClients.Add(trimmed);
        }
    }

    /// <summary>
    /// Loads the trusted player names from ANTICHEAT/trusted.txt.
    /// If the file doesn't exist, it creates one.
    /// </summary>
    void LoadTrustedList()
    {
        string folder = "ANTICHEAT";
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        string filePath = Path.Combine(folder, "trusted.txt");
        if (!File.Exists(filePath))
        {
            // Create an empty file.
            File.WriteAllText(filePath, "");
        }

        trustedPlayers.Clear();
        foreach (string line in File.ReadAllLines(filePath))
        {
            string trimmed = line.Trim().ToLowerInvariant();
            if (trimmed.Length > 0)
                trustedPlayers.Add(trimmed);
        }
    }

    /// <summary>
    /// Saves the current trusted player list to ANTICHEAT/trusted.txt.
    /// </summary>
    void SaveTrustedList()
    {
        string folder = "ANTICHEAT";
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);
        string filePath = Path.Combine(folder, "trusted.txt");
        File.WriteAllLines(filePath, trustedPlayers);
    }

    /// <summary>
    /// Saves the current cheat clients list to ANTICHEAT/CheatClients.txt.
    /// </summary>
    void SaveCheatClientsList()
    {
        string folder = "ANTICHEAT";
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);
        string filePath = Path.Combine(folder, "CheatClients.txt");
        File.WriteAllLines(filePath, cheatClients);
    }

    /// <summary>
    /// Called when a player connects.
    /// If anti-cheat is enabled and the player's client name contains any of the cheat client keywords
    /// (and the player is not trusted), a warning-and-kick sequence is started.
    /// </summary>
    void OnPlayerConnect(Player p)
    {
        if (!antiCheatEnabled)
            return;

        // If the player is trusted, skip anti-cheat check.
        if (trustedPlayers.Contains(p.truename.ToLowerInvariant()))
            return;

        string clientName = p.Session.ClientName();
        foreach (string cheatName in cheatClients)
        {
            if (clientName.IndexOf(cheatName, StringComparison.OrdinalIgnoreCase) != -1)
            {
                new Thread(() => WarnAndKick(p)).Start();
                break;
            }
        }
    }

    /// <summary>
    /// Sends warning messages to the player before kicking them, using timed delays.
    /// The final kick is executed from the console.
    /// </summary>
    void WarnAndKick(Player p)
    {
        string name = p.name;
        Command say = Command.Find("say");

        say.Use(p, name + " %3Appears To Be Using A Disallowed Client!");
        Thread.Sleep(3000);
        say.Use(p, name + " %3You Must Stop Using This Client Within The Next 15 Seconds Or You Will Be Kicked!");
        Thread.Sleep(6000);
        say.Use(p, name + " %3Last Warning: Please change your client or %cOps will be alerted!");
        Thread.Sleep(3000);
        say.Use(p, "Well... Enjoy The Kick %3" + name + "!");
        Thread.Sleep(3000);
        
        Command.Find("kick").Use(Player.Console, name + " You Have Been Kicked By The Anti-Cheat");
    }

    /// <summary>
    /// Command to toggle anti-cheat, add cheat client keywords, and trust players.
    /// Usage:
    ///   /anticheat           - toggles anti-cheat on/off
    ///   /anticheat add <text>   - adds a cheat client keyword to the list
    ///   /anticheat trust <name> - adds a trusted player (exempt from kick)
    /// </summary>
    sealed class CmdAntiCheat : Command2
    {
        public override string name { get { return "AntiCheat"; } }
        public override string shortcut { get { return ""; } }
        public override string type { get { return "mod"; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Operator; } }

        public override void Use(Player p, string message)
        {
            // If no additional parameters, toggle the anti-cheat.
            if (string.IsNullOrEmpty(message))
            {
                antiCheatEnabled = !antiCheatEnabled;
                Command say = Command.Find("say");
                if (say == null)
                    return;

                if (antiCheatEnabled)
                {
                    say.Use(p, "%3Anti-Cheat%f: %2ON");
                }
                else
                {
                    say.Use(p, "%3Anti-Cheat%f: %cOFF");
                }
                return;
            }

            // Split the message for subcommands.
            string[] args = message.Split(new char[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (args.Length < 2)
            {
                p.Message("Usage:");
                p.Message("  /anticheat add <client_keyword>");
                p.Message("  /anticheat trust <playername>");
                return;
            }

            string subcmd = args[0].ToLowerInvariant();
            string param = args[1].Trim();

            // Process the "add" subcommand.
            if (subcmd == "add")
            {
                if (!cheatClients.Contains(param))
                {
                    cheatClients.Add(param);
                    // Save changes.
                    SaveCheatClientsList();
                    p.Message("Cheat client keyword '{0}' has been added.", param);
                }
                else
                {
                    p.Message("Cheat client keyword '{0}' is already in the list.", param);
                }
            }
            // Process the "trust" subcommand.
            else if (subcmd == "trust")
            {
                string lowerName = param.ToLowerInvariant();
                if (!trustedPlayers.Contains(lowerName))
                {
                    trustedPlayers.Add(lowerName);
                    // Save changes.
                    SaveTrustedList();
                    p.Message("Player '{0}' has been added to the trusted list.", param);
                }
                else
                {
                    p.Message("Player '{0}' is already trusted.", param);
                }
            }
            else
            {
                p.Message("Unknown subcommand. Usage:");
                p.Message("  /anticheat add <client_keyword>");
                p.Message("  /anticheat trust <playername>");
            }
        }

        public override void Help(Player p)
        {
            p.Message("&T/anticheat");
            p.Message("&HToggles the anti-cheat system on or off.");
            p.Message("&H/anticheat add <client_keyword> &H- adds a cheat client keyword to the detection list.");
            p.Message("&H/anticheat trust <playername> &H- adds a player to the trusted list (exempt from detection).");
        }

        /// <summary>
        /// Saves the cheatClients list to file.
        /// </summary>
        void SaveCheatClientsList()
        {
            string folder = "ANTICHEAT";
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            string filePath = Path.Combine(folder, "CheatClients.txt");
            File.WriteAllLines(filePath, cheatClients);
        }

        /// <summary>
        /// Saves the trusted players list to file.
        /// </summary>
        void SaveTrustedList()
        {
            string folder = "ANTICHEAT";
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            string filePath = Path.Combine(folder, "trusted.txt");
            File.WriteAllLines(filePath, trustedPlayers);
        }
    }
}
