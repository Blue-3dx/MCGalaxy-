using System;
using System.IO;
using System.Collections.Generic;
using MCGalaxy;
using MCGalaxy.Commands;

public class RatePlugin : Plugin
{
    public override string name { get { return "RatePlugin"; } }
    public override string MCGalaxy_Version { get { return "1.9.3.8"; } }
    private const string ratingsDir = "ratings";
    private Dictionary<string, int> playerRatings = new Dictionary<string, int>();

    public override void Load(bool startup)
    {
        if (!Directory.Exists(ratingsDir)) Directory.CreateDirectory(ratingsDir);
        Command.Register(new CmdRate(this));
        LoadRatings();
    }

    public override void Unload(bool shutdown)
    {
        Command.Unregister(Command.Find("Rate"));
    }

    void LoadRatings()
    {
        for (int i = 1; i <= 10; i++)
        {
            string path = Path.Combine(ratingsDir, i.ToString() + ".txt");
            if (!File.Exists(path)) continue;

            foreach (string line in File.ReadAllLines(path))
            {
                int index = line.IndexOf(" : ");
                if (index == -1) continue;
                string player = line.Substring(0, index);
                playerRatings[player.ToLower()] = i;
            }
        }
    }

    public bool HasRated(string player) { return playerRatings.ContainsKey(player.ToLower()); }
    public void SaveRating(string player, int rating, string message)
    {
        string path = Path.Combine(ratingsDir, rating.ToString() + ".txt");
        using (StreamWriter writer = File.AppendText(path))
        {
            writer.WriteLine(player + " : " + message);
        }
        playerRatings[player.ToLower()] = rating;
    }
}

public class CmdRate : Command
{
    private RatePlugin plugin;
    public CmdRate(RatePlugin plugin) { this.plugin = plugin; }

    public override string name { get { return "Rate"; } }
    public override string shortcut { get { return ""; } }
    public override string type { get { return "other"; } }
    public override LevelPermission defaultRank { get { return LevelPermission.Guest; } }

    public override void Use(Player p, string message)
    {
        if (plugin.HasRated(p.name))
        {
            p.Message("%cYou have already rated the server!");
            return;
        }

        string[] args = message.Split(new char[] { ' ' }, 2);
        int rating;
        if (args.Length == 0 || !int.TryParse(args[0], out rating) || rating < 1 || rating > 10)
        {
            p.Message("%cInvalid usage! Use %T/Rate [1-10] [optional message]");
            return;
        }

        string msg = args.Length > 1 ? args[1] : "";
        plugin.SaveRating(p.name, rating, msg);
        p.Message("%aThank you for rating the server %b" + rating + "/10%a!");
    }

    public override void Help(Player p)
    {
        p.Message("%T/Rate [1-10] [optional message]");
        p.Message("%HRate the server from 1 to 10 with an optional message.");
        p.Message("%HYou can only rate once.");
    }
}
