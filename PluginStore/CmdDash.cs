using System;
using System.Collections.Generic;
using MCGalaxy;
using MCGalaxy.Commands;
using MCGalaxy.Maths;
using MCGalaxy.Network;

namespace MCGalaxy {
    public class PvpDash : Plugin {
        public override string name { get { return "PvpDash"; } }
        public override string MCGalaxy_Version { get { return "1.9.1.2"; } }
        public override bool LoadAtStartup { get { return true; } }
        public override string creator { get { return "Blue3dx"; } }

        public static bool DashEnabled = true;
        static Dictionary<string, DateTime> lastDashTime = new Dictionary<string, DateTime>();

        public override void Load(bool startup) {
            Command.Register(new CmdDash());
        }
        public override void Unload(bool shutdown) {
            Command.Unregister(Command.Find("dash"));
        }

        // Uses player's yaw to calculate forward direction (X and Z)
        static Vec3F32 GetDirection(Player p) {
            double deg = Orientation.PackedToDegrees(p.Rot.RotY);
            double rad = deg * Math.PI / 180.0;
            float x = (float)Math.Sin(rad);
            float z = (float)-Math.Cos(rad);
            return new Vec3F32(x, 0, z);
        }

        // Parses +dashcooldown=number from MOTD, returns cooldown in seconds
        static double GetDashCooldown(Level lvl) {
            if (lvl == null || lvl.Config == null || lvl.Config.MOTD == null) return 1.0;
            string motd = lvl.Config.MOTD.ToLower();

            // Look for +dashcooldown= in the MOTD
            int idx = motd.IndexOf("+dashcooldown=");
            if (idx == -1) return 1.0;

            idx += "+dashcooldown=".Length;
            int endIdx = idx;
            // Allow 0.x or 1.2 or 0, numbers only
            while (endIdx < motd.Length && (char.IsDigit(motd[endIdx]) || motd[endIdx] == '.'))
                endIdx++;

            string cooldownStr = motd.Substring(idx, endIdx - idx);
            double cooldown;
            if (!double.TryParse(cooldownStr, out cooldown)) return 1.0;
            // Clamp
            if (cooldown < 0) cooldown = 0;
            if (cooldown > 60) cooldown = 60; // sanity limit, but 0 disables cooldown
            if (cooldown > 0 && cooldown < 0.01) cooldown = 0.01; // minimum allowed (except 0 for "no limit")
            return cooldown;
        }

        public static void DoDash(Player p) {
            // Check for +dash in the MOTD (case-insensitive)
            if (p.level == null || p.level.Config == null || string.IsNullOrEmpty(p.level.Config.MOTD) ||
                !p.level.Config.MOTD.ToLower().Contains("+dash")) {
                p.Message("&cDashing is disabled!");
                return;
            }

            double cooldown = GetDashCooldown(p.level);

            if (!DashEnabled) {
                p.Message("%cDash is currently disabled.");
                return;
            }
            DateTime lastDash;
            if (cooldown > 0 && lastDashTime.TryGetValue(p.name, out lastDash)) {
                if ((DateTime.UtcNow - lastDash).TotalSeconds < cooldown) {
                    double timeLeft = cooldown - (DateTime.UtcNow - lastDash).TotalSeconds;
                    if (timeLeft > 0.01) p.Message("&cPlease wait {0:0.##}s before dashing again.", timeLeft);
                    return;
                }
            }
            lastDashTime[p.name] = DateTime.UtcNow;

            Vec3F32 dir = GetDirection(p);
            dir.Y = 0;
            if (dir.Length > 0.01f) dir = Vec3F32.Normalise(dir);

            float strength = 3.0f; // 3 blocks
            float velocity = (strength * 32) / 10f; // 10 ticks for 3 blocks

            if (p.Supports(CpeExt.VelocityControl)) {
                // Use mode=1 (replace) for X and Z, mode=0 (add) for Y (since we want to dash horizontally)
                p.Send(Packet.VelocityControl(
                    dir.X * velocity,  // X velocity
                    0.0f,              // Y velocity
                    dir.Z * velocity,  // Z velocity
                    1,                 // xmode (replace)
                    0,                 // ymode (add)
                    1));               // zmode (replace)
            } else {
                p.Message("Cannot dash: client lacks VelocityControl support.");
            }
        }
    }

    public sealed class CmdDash : Command2 {
        public override string name { get { return "dash"; } }
        public override string shortcut { get { return ""; } }
        public override string type { get { return "movement"; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Builder; } }

        public override void Use(Player p, string message) {
            PvpDash.DoDash(p);
        }

        public override void Help(Player p) {
            p.Message("&T/Dash");
            p.Message("&HDashes you 3 blocks forward in the direction you are facing.");
            p.Message("&HDashing must be enabled with +dash in the map's MOTD.");
            p.Message("&HChange dash cooldown by adding +dashcooldown=seconds (e.g. +dashcooldown=1 or 0.1 or 0 for no limit).");
            p.Message("&HMinimum cooldown is 0.01s (except for 0, which disables cooldown entirely).");
        }
    }
}