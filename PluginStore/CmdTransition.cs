using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using MCGalaxy;

public class CmdTransition : Command {
    // Directory and file under the server’s Config folder where enabled commands are stored
    const string DirName  = "TRANSITION";
    const string FileName = "usable.txt";

    // In‐memory list of enabled command names (all stored in lowercase)
    static readonly List<string> usableCommands = new List<string>();

    // Full path to the usable.txt file
    static readonly string usableFilePath;

    // Static constructor: ensure directory & file exist, then load commands
    static CmdTransition() {
        try {
            // Assume the server’s working directory has a “Config” folder:
            string configDir    = Path.Combine("Config", DirName);
            usableFilePath      = Path.Combine(configDir, FileName);

            if (!Directory.Exists(configDir)) {
                Directory.CreateDirectory(configDir);
            }
            if (!File.Exists(usableFilePath)) {
                File.WriteAllText(usableFilePath, "");
            }

            // Read each non‐empty line, trim, lowercase, and add to usableCommands
            foreach (string line in File.ReadAllLines(usableFilePath)) {
                string cmd = line.Trim().ToLowerInvariant();
                if (cmd.Length > 0 && !usableCommands.Contains(cmd)) {
                    usableCommands.Add(cmd);
                }
            }
        }
        catch {
            // Ignore any errors during initialization
        }
    }

    public override string name          { get { return "transition"; } }
    public override string shortcut      { get { return "";           } }
    public override string type          { get { return "mod";        } }
    public override LevelPermission defaultRank { get { return LevelPermission.Operator; } }
    public override bool museumUsable    { get { return false;        } }

    public override void Use(Player p, string message) {
        if (message == null) message = "";

        // 1) Check for "add" subcommand: "/transition add <commandName>"
        string[] firstSplit = message.Split(new char[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
        if (firstSplit.Length >= 1 && firstSplit[0].Equals("add", StringComparison.OrdinalIgnoreCase)) {
            // Must be exactly two parts: "add" and the commandName
            if (firstSplit.Length < 2) {
                p.Message("&cUsage: /transition add <commandName>");
                return;
            }
            if (p.Rank < LevelPermission.Owner) {
                p.Message("&cOnly Owner (or higher) may add usable commands.");
                return;
            }

            string cmdToAdd = firstSplit[1].Trim().ToLowerInvariant();
            Command found = Command.Find(cmdToAdd);
            if (found == null) {
                p.Message("&cCannot add \"{0}\": no such command exists.", cmdToAdd);
                return;
            }
            if (usableCommands.Contains(cmdToAdd)) {
                p.Message("&cCommand \"{0}\" is already enabled for /transition.", cmdToAdd);
                return;
            }

            try {
                File.AppendAllText(usableFilePath, cmdToAdd + Environment.NewLine);
                usableCommands.Add(cmdToAdd);
                p.Message("&aCommand \"{0}\" has been enabled for /transition.", cmdToAdd);
            }
            catch (Exception ex) {
                p.Message("&cFailed to add command: {0}", ex.Message);
            }
            return;
        }

        // 2) Otherwise, proceed with normal transition logic
        // Split into up to 9 parts:
        // [0]=R, [1]=G, [2]=B, [3]=targetAlpha,
        // [4]=skipAmount, [5]=intervalMs,
        // [6]=fadeOut, [7]=when, [8]=rest‐of‐command (optional)
        string[] parts = message.Split(new char[] { ' ' }, 9);
        if (parts.Length < 8) {
            Help(p);
            return;
        }

        // 3) Parse R, G, B, targetAlpha, skipAmount, intervalMs
        byte r, g, b, targetAlpha;
        int skipAmount, intervalMs;
        if (!byte.TryParse(parts[0], out r) ||
            !byte.TryParse(parts[1], out g) ||
            !byte.TryParse(parts[2], out b) ||
            !byte.TryParse(parts[3], out targetAlpha) ||
            !int.TryParse(parts[4], out skipAmount) ||
            !int.TryParse(parts[5], out intervalMs))
        {
            p.Message("&cUsage: /transition <R> <G> <B> <targetAlpha> <skipAmount> <intervalMs> <fadeOut> <when> [command...]");
            p.Message("&cR, G, B, targetAlpha must be 0–255. skipAmount & intervalMs must be positive integers.");
            return;
        }
        if (skipAmount <= 0) {
            p.Message("&cskipAmount must be greater than 0.");
            return;
        }
        if (intervalMs <= 0) {
            p.Message("&cintervalMs must be greater than 0.");
            return;
        }

        // 4) Parse fadeOut (true/false)
        bool fadeOutFlag;
        if (!bool.TryParse(parts[6], out fadeOutFlag)) {
            p.Message("&cfadeOut must be either true or false.");
            return;
        }

        // 5) Parse when ("now" or "after")
        string whenRaw = parts[7].ToLowerInvariant();
        bool runNow;
        if (whenRaw == "now") {
            runNow = true;
        } else if (whenRaw == "after") {
            runNow = false;
        } else {
            p.Message("&cThe <when> parameter must be either \"now\" or \"after\".");
            return;
        }

        // 6) Extract optional sub‐command (if provided)
        string afterCommand = "";
        if (parts.Length == 9) {
            afterCommand = parts[8].Trim();
        }

        Command cmd = null;
        string cmdName = "";
        string cmdArgs = "";

        if (afterCommand.Length > 0) {
            // Trim leading '/'
            string cmdLine = afterCommand.StartsWith("/") ? afterCommand.Substring(1) : afterCommand;
            string[] cmdParts = cmdLine.Split(new char[] { ' ' }, 2);
            cmdName = cmdParts[0].ToLowerInvariant();
            cmdArgs = (cmdParts.Length > 1) ? cmdParts[1] : "";

            // 7) Check that cmdName is in the enabled‐commands list
            if (!usableCommands.Contains(cmdName)) {
                p.Message("&cThe command \"{0}\" is not enabled for /transition. Use &e/transition add {0}&c to enable it.", cmdName);
                return;
            }

            // 8) Verify the command exists and that the player has permission
            cmd = Command.Find(cmdName);
            if (cmd == null) {
                p.Message("&cUnknown command: \"{0}\"", cmdName);
                return;
            }
            if (p.Rank < cmd.defaultRank) {
                p.Message("&cYou do not have permission to run /{0}.", cmdName);
                return;
            }
        }

        // 9) Validation passed: launch background thread to do the transition
        new Thread(delegate () {
            DoTransition(p, r, g, b, targetAlpha, skipAmount, intervalMs, fadeOutFlag, runNow, cmd, cmdArgs);
        }).Start();
    }

    static void DoTransition(Player p, byte r, byte g, byte b, byte targetAlpha,
                             int skipAmount, int intervalMs,
                             bool fadeOutFlag, bool runNow,
                             Command cmd, string cmdArgs)
    {
        try {
            byte alpha = 0;

            // --- FADE IN (0 → targetAlpha) ---
            while (true) {
                if (alpha > targetAlpha) alpha = targetAlpha;

                // Send raw‐bytes: [56, 0, 0, 0, R, G, B, alpha, 255, 255]
                byte[] packet = new byte[] { 56, 0, 0, 0, r, g, b, alpha, 255, 255 };
                p.Session.Send(packet);

                if (alpha == targetAlpha) break;

                Thread.Sleep(intervalMs);
                int next = alpha + skipAmount;
                alpha = (byte)(next > targetAlpha ? targetAlpha : next);
            }

            // At this point, alpha == targetAlpha.
            // If runNow==true and a sub‐command was provided, run it immediately.
            if (runNow && cmd != null) {
                cmd.Use(p, cmdArgs, new CommandData());
            }

            // --- EITHER FADE OUT --- or WAIT → RESET
            if (fadeOutFlag) {
                // Fade‐out loop: targetAlpha → 0
                while (true) {
                    if (alpha == 0) break;

                    int next = alpha - skipAmount;
                    alpha = (byte)(next < 0 ? 0 : next);

                    // Send raw‐bytes for new alpha
                    byte[] packet = new byte[] { 56, 0, 0, 0, r, g, b, alpha, 255, 255 };
                    p.Session.Send(packet);

                    if (alpha == 0) break;
                    Thread.Sleep(intervalMs);
                }

                // Fade‐out complete: if runNow==false (“after”), run sub‐command now
                if (!runNow && cmd != null) {
                    cmd.Use(p, cmdArgs, new CommandData());
                }

                // Ensure final reset to alpha=0 (transparent)
                byte[] resetPkt = new byte[] { 56, 0, 0, 0, r, g, b, 0, 255, 255 };
                p.Session.Send(resetPkt);
            }
            else {
                // No fade‐out: wait 1 second, then reset
                if (!runNow && cmd != null) {
                    // “after” case: wait 1s, then run sub‐command
                    Thread.Sleep(1000);
                    cmd.Use(p, cmdArgs, new CommandData());
                } else {
                    // runNow==true: we already ran it, so just wait 1s
                    Thread.Sleep(1000);
                }

                // Reset to transparent
                byte[] resetPkt = new byte[] { 56, 0, 0, 0, r, g, b, 0, 255, 255 };
                p.Session.Send(resetPkt);
            }
        }
        catch {
            // Silently ignore errors on this thread
        }
    }

    public override void Help(Player p) {
        p.Message("&HUsage:");
        p.Message("&e /transition add <commandName>");
        p.Message("&7  • Adds <commandName> to the list of commands enabled for /transition. Only Owner+ may do this.");
        p.Message("&e /transition <R> <G> <B> <targetAlpha> <skipAmount> <intervalMs> <fadeOut> <when> [command...]");
        p.Message("&7  • <R> <G> <B> <targetAlpha>: 0–255 each.");
        p.Message("&7  • <skipAmount>: how many alpha points to change per step (integer > 0).");
        p.Message("&7  • <intervalMs>: how many milliseconds between each alpha step (> 0).");
        p.Message("&7  • <fadeOut>: true or false. If true, fades back down to 0; if false, stays at <targetAlpha> for 1 s, then resets.");
        p.Message("&7  • <when>: now or after.");
        p.Message("&7     – now: run [command] immediately when targetAlpha is reached.");
        p.Message("&7     – after: run [command] after fade‑out (or 1 s if no fade‑out).");
        p.Message("&7  • [command...] (optional): must be in TRANSITION/usable.txt (use &e/transition add <cmd>&7 to enable).");
        p.Message("&HExamples:");
        p.Message("&e /transition add kick");
        p.Message("&7  → Enables “kick” so you can later do “/transition … kick PlayerName.”");
        p.Message("&e /transition 255 0 0 255 10 50 true now kick BadPlayer");
        p.Message("&7  → Fade to red, run “/kick BadPlayer” immediately, then fade back to transparent.");
        p.Message("&e /transition 0 0 255 200 5 100 false after say Hello!");
        p.Message("&7  → Fade to blue (alpha=200), wait 1 s, then run “/say Hello!”, then reset.");
    }
}
