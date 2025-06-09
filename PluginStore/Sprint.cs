//reference System.Core.dll

using System;
using System.Linq;

using MCGalaxy.Commands;
using MCGalaxy.Events;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Network;
using MCGalaxy.Tasks;

namespace MCGalaxy
{
    public class Sprinting : Plugin
    {
        public override string name { get { return "Sprinting"; } }
        public override string MCGalaxy_Version { get { return "1.9.3.0"; } }
        public override string creator { get { return "Blue 3dx"; } }

        public override void Load(bool startup)
        {
            OnGettingMotdEvent.Register(HandleGettingMOTD, Priority.High);

            Command.Register(new CmdSprint());
        }

        public override void Unload(bool shutdown)
        {
            OnGettingMotdEvent.Unregister(HandleGettingMOTD);

            Command.Unregister(Command.Find("Sprint"));
        }

        static void HandleGettingMOTD(Player p, ref string motd)
        {
            Player[] players = PlayerInfo.Online.Items;
            foreach (Player pl in players)
            {
                if (!p.Supports(CpeExt.TextHotkey)) continue;
                pl.Send(Packet.TextHotKey("Sprint", "/Sprint◙", 28, 0, true)); // Left Shift key (42)
            }

            // Check if player has actually toggled sprinting, since defaults to false
            if (!p.Extras.GetBoolean("IS_SPRINTING")) return;

            // Remove current horspeed rule to avoid conflicts
            motd = motd
                   .SplitSpaces()
                   .Where(word => !word.CaselessStarts("horspeed="))
                   .Join(" ");

            motd += " horspeed=1.47";
        }

        public override void Help(Player p)
        {
        }
    }

    public sealed class CmdSprint : Command2
    {
        public override string name { get { return "Sprint"; } }
        public override string type { get { return CommandTypes.Games; } }
        public override bool museumUsable { get { return false; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }
        public override bool SuperUseable { get { return false; } }

        public override void Use(Player p, string message, CommandData data)
        {
            if (p.Extras.GetBoolean("IS_SPRINTING"))
            {
                p.Extras["IS_SPRINTING"] = false;
                p.Extras["HAS_SPRINTED"] = true;
                p.SendMapMotd();
                Command.Find("SilentModel").Use(p, "humanoid|1");
            }
            else
            {
                p.Extras["IS_SPRINTING"] = true;
                p.Extras["HAS_SPRINTED"] = true;
                p.SendMapMotd();
            }
        }

        public override void Help(Player p)
        {
            p.Message("%T/Sprint %H- Toggles sprinting.");
        }
    }
}
